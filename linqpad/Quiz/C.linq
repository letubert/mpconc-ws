<Query Kind="Statements" />

//  Do you think that the code prints "Failed"? 


async void ThrowExceptionAsync()
{
	throw new InvalidOperationException();
}

void CallThrowExceptionAsync()
{
	try
	{
		ThrowExceptionAsync();
	}
	catch (Exception)
	{
		Console.WriteLine("Failed");
	}
}


CallThrowExceptionAsync();



















// The exception is not handled because ThrowExceptionAsync starts the work and returns immediately  
// and the exception happens somewhere on a background thread.
// async void methods can be useful when you're writing an event handler. 
// Event handlers should return void and they often start some work that continues in background.