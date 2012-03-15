namespace ExampleRunner
{
  using System;
  using System.Configuration;
  using System.Reflection;
  using resque;

  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine(resque.DummyJob.assemblyQualifiedName());

      Type t = typeof(DummyJob);
      Assembly.GetExecutingAssembly();

      Console.WriteLine(t.AssemblyQualifiedName);
      const string assemblyQualification = ", ExampleRunner, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
      Resque.setAssemblyQualifier(assemblyQualification);

      String server = ConfigurationManager.AppSettings["redis-host"];
      String port = ConfigurationManager.AppSettings["redis-port"] ?? "6379";

      Resque.setRedis(new Redis(server, Convert.ToInt32(port)));
      Job.create("jobs", "DummyJob", "foo", 20, "bar");
      Worker w = new Worker("*");
      w.work(1);
    }
  }
}
