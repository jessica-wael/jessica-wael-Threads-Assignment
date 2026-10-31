
using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Phase3;



await new Phase1Runner().RunAsync();


await new Phase2Runner().RunAsync(); 

await new Phase3Runner().RunAsync();
await new FireAndForgetDemo(new EmailService()).RunAsync();

await new SyncVsAsyncBenchmark().RunAsync();