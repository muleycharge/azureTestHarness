$configs = @{ 
    appInsightsKey="6fb3df96-1ad9-47c4-908c-7b5304f6ef0b";
    serverResourceId="/subscriptions/ecb4f63c-b0e0-4b21-9227-a58fd02cc035/resourceGroups/zkj-test-group/providers/Microsoft.Web/serverfarms/zkjAzureTest-server";
    serviceBusConnection="Endpoint=sb://zkjazuretest-messaging.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xyaQMJBzuwDBC7PiZA5CobuZPjh+cp3IzaUL7d/dhXU=";
    siteHost="zkjazuretest-web.azurewebsites.net";
    siteName="zkjAzureTest-web";
    siteResourceId="/subscriptions/ecb4f63c-b0e0-4b21-9227-a58fd02cc035/resourceGroups/zkj-test-group/providers/Microsoft.Web/sites/zkjAzureTest-web";
    storageConnectionString="DefaultEndpointsProtocol=https;AccountName=zkjazureteststorage;AccountKey=rjwi8zhNvl7/02Gym0QdB2XCj4uhRVEisW4yDzUbGFdWd7GZOHYsBd6EzEUXcZU8CKaCw3jVkawHo1EMZHec9w==";
    storageContainerName="test-blobs";
    test1Subscription1ResourceId="/subscriptions/ecb4f63c-b0e0-4b21-9227-a58fd02cc035/resourceGroups/zkj-test-group/providers/Microsoft.ServiceBus/namespaces/zkjAzureTest-messaging/topics/test1-topic1/subscriptions/test1-sub";
    test1Subscription2ResourceId="/subscriptions/ecb4f63c-b0e0-4b21-9227-a58fd02cc035/resourceGroups/zkj-test-group/providers/Microsoft.ServiceBus/namespaces/zkjAzureTest-messaging/topics/test1-topic2/subscriptions/test1-sub";
    test1Topic1Name="test1-topic1";
    test1Topic2Name="test1-topic2";
    test2Subscription1ResourceId="/subscriptions/ecb4f63c-b0e0-4b21-9227-a58fd02cc035/resourceGroups/zkj-test-group/providers/Microsoft.ServiceBus/namespaces/zkjAzureTest-messaging/topics/test2-topic/subscriptions/test2-sub1";
    test2Subscription2ResourceId="/subscriptions/ecb4f63c-b0e0-4b21-9227-a58fd02cc035/resourceGroups/zkj-test-group/providers/Microsoft.ServiceBus/namespaces/zkjAzureTest-messaging/topics/test2-topic/subscriptions/test2-sub2";
    test2TopicName="test2-topic"};
$messageCount = 1000

$sendMessageJob = Start-ThreadJob -ScriptBlock {
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


Write-Host "waiting"
$sendMessageJob | Wait-Job
Write-Host "receiving"
$sendMessageJob | Receive-Job 