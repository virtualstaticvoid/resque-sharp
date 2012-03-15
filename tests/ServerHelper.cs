namespace resque
{
  using System;
  using System.Configuration;

  static class ServerHelper
  {

    public static Redis GetRedis()
    {
      String server = ConfigurationManager.AppSettings["redis-host"];
      String port = ConfigurationManager.AppSettings["redis-port"] ?? "6379";

      return new Redis(server, Convert.ToInt32(port));
    }

  }
}
