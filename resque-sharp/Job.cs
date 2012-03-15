﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace resque
{
  public class Job
  {
    public Dictionary<string, object> payload { get; set; }
    public string queue { get; set; }
    public Worker worker { get; set; }

    public Job()
    {
      throw new NotImplementedException();
    }

    public Job(string queue, Dictionary<string, object> payload)
    {
      this.queue = queue;
      this.payload = payload;
    }

    public Type PayloadClass()
    {
      string className = (string)payload["class"];
      if (Resque.getAssemblyQualifier() != null)
      {
        className += Resque.getAssemblyQualifier();
      }

      return Type.GetType(className, true);
    }

    public static bool create(string queue, string className, params object[] args)
    {
      if (String.IsNullOrEmpty(className))
      {
        throw new NoClassError();
      }
      Resque.Push(queue, new Dictionary<String, Object>() { { "class", className }, { "args", args } });
      return true;
    }

    internal static Job Reserve(string queue)
    {
      Dictionary<string, object> payload = Resque.Pop(queue);
      if (payload == null)
        return null;
      return new Job(queue, payload);

    }

    internal void perform()
    {

      System.Reflection.MethodInfo methodInfo = PayloadClass().GetMethod("perform", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy);
      if (methodInfo == null)
        throw new NotImplementedException();
      object[] parameters = new object[1] { args().ToArray() };
      methodInfo.Invoke(null, parameters);

    }

    public ArrayList args()
    {
      ArrayList list = new ArrayList();
      JArray args = (JArray)payload["args"];
      foreach (JValue o in args)
      {
        list.Add(o.Value);
      }
      return list;
    }

    public void recreate()
    {
      Job.create(queue, PayloadClass().FullName, args().ToArray());
    }

    public override bool Equals(object other)
    {
      if (other == null)
        return false;
      if (object.ReferenceEquals(this, other))
        return true;
      if (this.GetType() != other.GetType())
        return false;
      Job job = (Job)other;
      return (this.queue == job.queue && this.PayloadClass() == job.PayloadClass() && arrayListElementsAreEqual(args(), job.args()));
    }

    public override int GetHashCode()
    {
      // based on the equality
      return this.queue.GetHashCode() + this.PayloadClass().GetHashCode() + args().GetHashCode();
    }

    private bool arrayListElementsAreEqual(ArrayList list, ArrayList otherList)
    {
      if (list.Count != otherList.Count)
      {
        return false;
      }
      int i = 0;
      foreach (object o in list)
      {
        if (!o.Equals(otherList[i]))
        {
          return false;
        }
        i++;
      }
      return true;
    }

    internal void fail(Exception e)
    {
      Failure.Redis failure = new Failure.Redis(e, worker, queue, payload);
      failure.save();
    }
  }


  public class DummyJob
  {
    public static string queue()
    {
      return "tester";
    }
    public static void perform(params object[] args)
    {
      Console.WriteLine("This is the dummy job reporting in");
    }

    public static string assemblyQualifiedName()
    {
      return System.Reflection.Assembly.GetExecutingAssembly().FullName;
    }
    //public DummyJob(string queue, Dictionary<string,object> dictionary) : base(queue, dictionary)
    //{

    //}
    // for testing
  }
  public class NotDummyJob
  {
    public static string queue()
    {
      return "tester";
    }
    public static void perform(params object[] args)
    {
      Console.WriteLine("This is the not dummy job reporting in");
    }
  }

  public class BadJob
  {
    public static string queue()
    {
      return "tester";
    }
    public static void perform(params object[] args)
    {
      throw new Exception("Bad Job!!");
    }
  }

  public class GoodJob
  {
    public static string queue()
    {
      return "tester";
    }
    public static void perform(params object[] args)
    {
      System.Threading.Thread.Sleep(1000);
      return;
    }
  }

  public class UninferrableInvalidJob
  {
  }

}
