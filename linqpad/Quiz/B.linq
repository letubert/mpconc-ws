<Query Kind="Statements">
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

// Guess what happens when you run the following asynchronous method


async Task Handler()
{
	Console.WriteLine("Before");
	Task.Delay(1000);
	Console.WriteLine("After");
}


Handler().Wait();




























// Were you expecting that it prints "Before", waits 1 second and then prints "After"? Wrong! 
// It prints both messages immediately without any waiting in between. 
// The problem is that Task.Delay returns a Task and we forgot to await until it completes using await.