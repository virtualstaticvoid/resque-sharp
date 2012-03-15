﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Collections;
using System.Text.RegularExpressions;

namespace resque
{
  public class NoQueueError : Exception { }

  public class NoClassError : Exception { }

  public class Resque
  {
    private static string staticAssemblyQualifier;
    private static Redis staticRedis;
    private static Failure.Failure Failure;

    public static string getAssemblyQualifier()
    {
      return staticAssemblyQualifier;
    }


    public static void setAssemblyQualifier(string assemblyQualifier)
    {
      staticAssemblyQualifier = assemblyQualifier;
    }

    public static Failure.Failure failure
    {
      get { return Failure ?? (Failure = new Failure.Failure(typeof (Failure.Redis))); }
      set
      {
        Failure = value;
      }
    }
    public static void setRedis(Redis redis)
    {
      staticRedis = redis;
    }
    public static Redis redis()
    {
      return staticRedis ?? (staticRedis = new Redis());
    }

    public static Worker[] working()
    {
      return Worker.working();
    }

    public static Worker[] workers()
    {
      return Worker.all();
    }

    public static void removeWorker(string workerId)
    {
      Worker.find(workerId).unregisterWorker();
    }

    public static bool Push(string queue, object item)
    {
      watchQueue(queue);
      redis().RightPush("resque:queue:" + queue, encode(item));
      return true;
    }

    public static Dictionary<string, object> Pop(string queue)
    {
      var data = redis().LeftPop("resque:queue:" + queue);
      return decodeData(data);
    }



    public static Dictionary<string, object> Peek(string queue)
    {
      var data = redis().ListIndex("resque:queue:" + queue, 0);
      return decodeData(data);
    }

    public static Dictionary<string, object> Peek(string queue, int start)
    {
      var resultData = redis().ListRange("resque:queue:" + queue, start, start);
      return resultData.Length == 0 ? null : decodeData(resultData[0]);
    }

    public static ArrayList Peek(string queue, int start, int count)
    {
      ArrayList results = new ArrayList();
      if (count == 1)
      {
        results.Add(Peek(queue, start));
      }
      else
      {
        foreach (byte[] data in redis().ListRange("resque:queue:" + queue, start, start + count - 1))
        {
          results.Add(decodeData(data));
        }
      }
      return results;
    }

    public static void RemoveQueue(string queue)
    {
      redis().RemoveFromSet("resque:queues", Encoding.UTF8.GetBytes(queue));
      redis().Remove("resque:queue:" + queue);
    }

    private static void watchQueue(string queue)
    {
      redis().AddToSet("resque:queues", queue);
    }

    public static string[] queues()
    {
      byte[][] rawResults = redis().GetMembersOfSet("resque:queues");
      if (rawResults.Length == 0)
        return new string[0];
      string[] results = new string[rawResults.Length];
      int i = 0;
      foreach (byte[] data in rawResults)
      {
        results[i] = Encoding.UTF8.GetString(data);
        i++;
      }
      return results;
    }



    public static Job Reserve(string queue)
    {
      return Job.Reserve(queue);
    }



    public static int size(string queue)
    {
      return redis().ListLength("resque:queue:" + queue);
    }

    public static string[] keys()
    {
      var mungedKeys = from k in redis().Keys
                       where Regex.Match(k, "^resque:").Success
                       select Regex.Replace(k, "^resque:", "");

      return mungedKeys.ToArray();
    }

    public static bool enqueue(string className, params object[] args)
    {
      Type workerType = Type.GetType(className);
      System.Reflection.MethodInfo methodInfo = workerType.GetMethod("queue", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.FlattenHierarchy);
      if (methodInfo == null)
        throw new NoQueueError();
      string queue = (string)methodInfo.Invoke(null, null);
      if (String.IsNullOrEmpty(queue))
        throw new NoQueueError();
      return Job.create(queue, className, args);
    }



    #region encoding
    public static string encode(object item)
    {
      return JsonConvert.SerializeObject(item);
    }

    public static object decode(string json)
    {
      return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
    }
    public static object decode(byte[] json)
    {
      return decode(Encoding.UTF8.GetString(json));
    }

    private static Dictionary<string, object> decodeData(byte[] data)
    {
      return data == null ? null : (Dictionary<string, object>) decode(data);
    }

    #endregion

  }

  public class info
  {
    /*
          :pending => queues.inject(0) { |m,k| m + size(k) },
          DONE :processed => Stat[:processed],
          DONE :queues => queues.size,
          DONE :workers => workers.size.to_i,
          DONE :working => working.size,
          DONE :failed => Stat[:failed],
          DONE :servers => [redis.server]
     */
    public static int Workers
    {
      get { return Resque.workers().Length; }
    }

    public static int Processed
    {
      get { return Stat.get("processed"); }
    }

    public static int Failed
    {
      get { return Stat.get("failed"); }
    }

    public static string Servers
    {
      get { return Resque.redis().Host; }
    }

    public static int Queues
    {
      get { return Resque.queues().Length; }
    }

    public static int Working
    {
      get { return Resque.working().Length; }
    }

    public static string[] Pending
    {
      get { return Resque.queues(); }
    }

  }
}
