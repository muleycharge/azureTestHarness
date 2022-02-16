param(
    [string] $groupName = "zkj-test-group",
    [string] $appName = "zkjAzureTest",
    [string] $subscription = "Development",
    [validateset("all", "group-deploy", "code-deploy", "code-build")]
    [string[]] $deployFlags
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$flags = $deployFlags
# Have to run template deploy to get connection strings
if(-not $flags)
{
    Write-Host "Using default resource deployment" -ForegroundColor Green
    $pingUrl = "https://$appName-web.azurewebsites.net/api/ping"
    $ping = Invoke-WebRequest -Uri $pingUrl
    if($ping.StatusCode -ne 204)
    {
        Write-Host "Status $($ping.StatusCode) received from $pingUrl running full deployment" -ForegroundColor Orange
        $flags = @( "all" )
    }
    else {
        Write-Host "Ping successful on $pingUrl, running minimal deployment" -ForegroundColor Green
        # Required in order to get connection strings & resource IDs
        $flags = @( "template-deploy" )
    }
}
elseif ($flags -notcontains "all"){
    $flags += "template-deploy"
}

# Scale every 2 minutes
$triggerScaleingIntervalilliseconds = 60000
$totalMessages = 5000

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

$logFile = "logs\out_$(get-date -format yyyyMMddHHmm).txt"
Write-Host "Sending messages" -ForegroundColor Green
$sendMessageJob = Start-ThreadJob -ScriptBlock {
    param($mc, $c)
    $sw = [system.diagnostics.stopwatch]::StartNew()
    .\Deploy\ServiceBusSender\ServiceBusSender.exe --message-count $mc --storage-conn $c.storageConnectionString --storage-container $c.storageContainerName --service-bus-conn $c.serviceBusConnection --topic-name $c.testTopic1Name --insights-key $c.appInsightsKey >> $logFile
    $sw.Stop();
    Write-Host "All messages sent. Elapsed seconds $($sw.Elapsed)" -ForegroundColor Green
} -ArgumentList @($messageCount, $configs)

$stopwatch =  [system.diagnostics.stopwatch]::StartNew()
$scaleTimer = [system.diagnostics.stopwatch]::StartNew()
$subShow = $null
$workers = 2
$startingUp = $true

while($messageCount -ne 0){
    Start-Sleep -Seconds $sleepTime

    $subShow = az servicebus topic subscription show --ids $configs.serviceBusSubscription2ResourceId | convertfrom-json
    $messageCount = $subShow.countDetails.activeMessageCount
    Write-Host "Messages remaining $messageCount. Elapsed seconds $($stopwatch.Elapsed)" -ForegroundColor Green

    # Trigger app service plan scaling, this will periodically trigger orphaned messages.
    if($scaleTimer.ElapsedMilliseconds -gt $triggerScaleingIntervalilliseconds){
        $scaleTimer.Restart()
        #Write-Host "Restarting app service" -ForegroundColor Green
        #az webapp restart -ids $configs.siteResourceId
        if($workers -eq 2){
            Write-Host "Scaling appservice plan from 2 workers to 8" -ForegroundColor Green
            $workers = 8
        } 
        else {
            Write-Host "Scaling appservice plan from 8 workers to 2" -ForegroundColor Green
            $workers = 2
        }
        az webapp scale --ids $configs.siteResourceId --instance-count $workers | Out-Null
    }

    if($startingUp -and $messageCount -eq 0){
        # Message count will be 0 at first while we wait for ServiceBusSender to spin up
        $messageCount = -1
    }
    else {
        $startingUp = $false
    }
}

$sendMessageJob | Wait-Job | Receive-Job

Write-Host "All messages processed. Elapsed seconds $($stopwatch.Elapsed)" -ForegroundColor Green
$subShow.countDetails | out-host