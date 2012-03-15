using System;

namespace ExampleRunner
{
  class DummyJob
  {
    public static string queue()
    {
      return "jobs";
    }
    public static void perform(params object[] args)
    {
      Console.WriteLine("This is the dummy job reporting in");
    }
  }
}