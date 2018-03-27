<Query Kind="Statements">
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

// does the folling code run asynchronously?

async Task FooAsync()
{
	await Task.Delay(1000);
}
void Handler()
{
	FooAsync().Wait();
}

Handler();























// we are calling FooAsync().Wait(). This means that we create a task and then, using Wait, block until it completes. 
// Simply removing Wait fixes the problem, because we just want to start the task.