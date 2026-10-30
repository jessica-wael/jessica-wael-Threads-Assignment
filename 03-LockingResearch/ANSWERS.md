# Assignment 03 — Answers

Student name:  Jessica Wael Khalaf
Group / ID:  3


1. System.Threading.Monitor it's like (Lock) built-in security guard :: it makes sure that only one thread at a time runs a piece of code,
   it prevents race conditions when multiple threads access shared data.

2. Lock compiles to a Monitor class... Monitor.Enter and Monitor.Exit inside a Try-Finally block

3. Syntax in Lock is simple, but in Monitor is explicit.
   Try/Catch in Lock is automatic , but in Monitor is Manual.
   Lock can only wait until the lock is free, Monitor can use Timeout(TryEnter)

4. Monitor.TryEnter: we use it when need a thread didn't wait forever ,if the lock is busy, skip or give up after a timeout.
   Monitor.Pulse : we use it when have a thread is waiting for something to happen and we want to alert it that a change has occurred 
   Monitor.Wait : we use it when need a thread needs to wait a specific condition is happened

5. private  readonly object _locked = new();
   private static int _counter;

    //With Lock::
staitc void withLock{    
  lock(_locked){
     _counter ++;
  }
}
-----------------------------
//With Monitor ::
static void WithMonitor()
{
   bool taken = false;
   try
   {
      Monitor.Enter(_locked, ref taken);
      _counter++;
   }
   finally
   {
     if (taken){
      Monitor.Exit(_locked);
     }
   }
}


-------------------------------
Links:
1- https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor?view=net-10.0

2-https://dev.to/rasulhsn/under-the-hood-of-thread-synchronization-with-lock-3h50

3- https://www.tutorialspoint.com/article/what-is-the-difference-between-monitor-and-lock-in-chash


-
