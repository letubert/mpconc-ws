<Query Kind="Statements">
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

// how to parallelize 
await Task.Delay(1000);
"Task 1 Completed".Dump();
await Task.Delay(1000);
"Task 2 Completed".Dump();



























//var task1 = Task.Delay(1000);
//var task2 = Task.Delay(1000);
//await task1;
//await task2;
//"Task 1 Completed".Dump();
//"Task 2 Completed".Dump();