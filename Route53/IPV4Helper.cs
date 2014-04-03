using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using Amazon.Route53;
using Amazon.Route53.Model;
using System.Net;

namespace Route53
    {
    public class IPV4Helper
        {
        public static string GetIP4Address()
            {
            string IP4Address = String.Empty;

            foreach (IPAddress address in Dns.GetHostAddresses(HttpContext.Current.Request.UserHostAddress))
                {
                if (address.AddressFamily.ToString() == "InterNetwork")
                    {
                    IP4Address = address.ToString();
                    break;
                    }
                }

            if (IP4Address != String.Empty)
                {
                return IP4Address;
                }

            foreach (IPAddress address in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                if (address.AddressFamily.ToString() == "InterNetwork")
                    {
                    IP4Address = address.ToString();
                    break;
                    }
                }

            return IP4Address;
            }
        }
    }