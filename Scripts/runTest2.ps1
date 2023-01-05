param(
    [string] $groupName = "zkj-test-group",
    [string] $appName = "zkjAzureTest",
    [string] $subscription = "Development",
    [validateset("all", "group-deploy", "code-deploy", "code-build")]
    [string[]] $deployFlags
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$pingUrl = "https://$appName-web.azurewebsites.net/api/ping"
# Scale every 2 minutes
$triggerScaleingIntervalilliseconds = 120000
$totalMessages = 30000
$maxRetry = 3

function main
{
    $flags = $deployFlags
    # Have to run template deploy to get connection strings
    if(-not $flags)
    {
        Write-Host "Using default resource deployment" -ForegroundColor Green
        $ping = Invoke-WebRequest -Uri $pingUrl -SkipHttpErrorCheck
        if($ping.StatusCode -ne 204)
        {
            Write-Host "Status $($ping.StatusCode) received from $pingUrl running full deployment" -ForegroundColor DarkYellow
            $flags = @( "all" )
        }
        else {
            Write-Host "Ping successful on $pingUrl, running minimal deployment" -ForegroundColor Green
            # Required in order to get connection strings & resource IDs
            $flags = @( "template-deploy" )
        }
    }
    elseif ($flags -notcontains "all"){
        # Must at least run template deploy so that we get configuration output.
        $flags += "template-deploy"
    }


    $messageCount = $totalMessages
    $sleepTime = [Math]::Ceiling( $totalMessages / 1000 )

    $configs = .\deploy.ps1 -groupName $groupName -appName $appName -subscription $subscription -deployFlags $flags
    $configs | out-host

    $pingUrl = "https://$($configs.siteHost)/api/ping"
    $ping = Invoke-WebRequest -Uri $pingUrl

    if($ping.StatusCode -ne 204)
    {
        throw "Status $($ping.StatusCode) received from $pingUrl. Deployment failed."
    }

    Write-Host "Sending messages Test 1" -ForegroundColor Green
    $sendMessageJob1 = Start-ThreadJob -ScriptBlock {
        param($mc, $c)
        $sw = [system.diagnostics.stopwatch]::StartNew()
        .\Deploy\ServiceBusSender\ServiceBusSender.exe `
            --test-case Test1 `
            --message-count $mc `
            --storage-conn $c.storageConnectionString `
            --storage-container $c.storageContainerName `
            --service-bus-conn $c.serviceBusConnection `
            --test1-topic-name $c.Test1Topic1Name `
            --test2-topic-name $c.Test2TopicName `
            --insights-key $c.appInsightsKey >> $logFile
        $sw.Stop();
        Write-Host "All messages sent. Elapsed seconds $($sw.Elapsed)" -ForegroundColor Green
    } -ArgumentList @($messageCount, $configs)

    Write-Host "Sending messages Test 2" -ForegroundColor Green
    $sendMessageJob2 = Start-ThreadJob -ScriptBlock {
        param($mc, $c)
        $sw = [system.diagnostics.stopwatch]::StartNew()
        .\Deploy\ServiceBusSender\ServiceBusSender.exe `
            --test-case Test2 `
            --message-count $mc `
            --storage-conn $c.storageConnectionString `
            --storage-container $c.storageContainerName `
            --service-bus-conn $c.serviceBusConnection `
            --test1-topic-name $c.Test1Topic1Name `
            --test2-topic-name $c.Test2TopicName `
            --insights-key $c.appInsightsKey >> $logFile
        $sw.Stop();
        Write-Host "All messages sent. Elapsed seconds $($sw.Elapsed)" -ForegroundColor Green
    } -ArgumentList @($messageCount, $configs)

    $stopwatch =  [system.diagnostics.stopwatch]::StartNew()
    $scaleTimer = [system.diagnostics.stopwatch]::StartNew()
    $workers = 2
    $retry = 0

    while($messageCount -ne 0 -and $retry -lt $maxRetry){
        Start-Sleep -Seconds $sleepTime

        # Get message counts
        $messageCount = getMessageCount

        # Wait for ServiceBusSender to put messages on the queue or to fail before exiting.
        if($messageCount -eq 0 -and $sendMessageJob2.State -ne "Completed"){
            Write-Host "Waiting for ServiceBusSender to enqueue messages or fail" -ForegroundColor Green
        }
        # Retry once message count is 0 in case there are stragglers
        elseif($messageCount -eq 0){
            Write-Host "No messages in queue will wait $(($maxRetry - $retry) * $sleepTime) seconds before exiting"
            $retry++
        }
        else
        {
            $retry = 0
            Write-Host "Messages remaining $messageCount. Elapsed time $($stopwatch.Elapsed)" -ForegroundColor Green
        }

        # Trigger app service plan scaling, this will periodically trigger orphaned messages.
        if($scaleTimer.ElapsedMilliseconds -gt $triggerScaleingIntervalilliseconds){
            $scaleTimer.Restart()
            #Write-Host "Restarting app service" -ForegroundColor Green
            #az webapp restart --ids $configs.siteResourceId
            
            if($workers -eq 2){
                Write-Host "Scaling Webapp from 2 instances to 8" -ForegroundColor Green
                $workers = 8
            } 
            else {
                Write-Host "Scaling Webapp from 8 instances to 2" -ForegroundColor Green
                $workers = 2
            }
            az webapp scale --ids $configs.siteResourceId --instance-count $workers | Out-Null
        }
    }

    Write-Host "Job 1"
    $sendMessageJob1 | Wait-Job | Receive-Job
    Write-Host "Job 2"
    $sendMessageJob2 | Wait-Job | Receive-Job 

    Write-Host "All messages processed. Elapsed seconds $($stopwatch.Elapsed)" -ForegroundColor Green
}

function getMessageCount
{
    $subShow = az servicebus topic subscription show --ids $configs.test2Subscription1ResourceId | convertfrom-json
    $messageCount = $subShow.countDetails.activeMessageCount
    $subShow = az servicebus topic subscription show --ids $configs.test2Subscription2ResourceId | convertfrom-json
    $messageCount += $subShow.countDetails.activeMessageCount

    return $messageCount
}

main