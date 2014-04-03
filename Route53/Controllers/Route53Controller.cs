using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using Amazon.Route53;
using Amazon.Route53.Model;
using System.Net;


/*
 * add auth
 * add logging
 * package
 * move params to object 
 */


namespace Route53.Controllers
{
    public class Route53Controller : Controller
    {
        public ActionResult UpdateRecord(String user, String password, String domain = "", String record = "")
        {
            ValidateParams(user,  password, ref domain, ref record);

            AmazonRoute53Client client = new AmazonRoute53Client();
            ListHostedZonesResponse hostedZonesResponce = client.ListHostedZones();
            String zoneId = (from zone in hostedZonesResponce.ListHostedZonesResult.HostedZones
                             where zone.Name == domain + "."
                             select zone.Id).First();

            ResourceRecordSet resourceRecordSetTarget = GetTargetResourceRecordsSet(domain, record, client, zoneId);

                if (resourceRecordSetTarget != null &&
                    resourceRecordSetTarget.ResourceRecords[0].Value != IPV4Helper.GetIP4Address())
            {
                ChangeResourceRecordSetsResponse responce = DeleteRecord(domain, record, client, zoneId,
                                                                                 resourceRecordSetTarget);
                WaitForChange(responce.ChangeResourceRecordSetsResult.ChangeInfo.Id, client);
            }

                 if ((resourceRecordSetTarget == null) || ( resourceRecordSetTarget.ResourceRecords[0].Value != IPV4Helper.GetIP4Address()))
            {

                ChangeResourceRecordSetsResponse responce = CreateRecord(domain, record, client, zoneId);
                WaitForChange(responce.ChangeResourceRecordSetsResult.ChangeInfo.Id, client);
            }


            ResourceRecordSet resourceRecordSetAdjusted = GetTargetResourceRecordsSet(domain, record, client, zoneId);

            ViewData["adjustedIP"] = resourceRecordSetAdjusted.ResourceRecords[0].Value;
            return View();
        }

        private void ValidateParams(String user, String password, ref String domain, ref String record)
        {
            if (String.IsNullOrEmpty(user) || String.IsNullOrEmpty(password))
            {
                throw new Exception("User or password cannot be empty");
            }
            else if (user != ConfigurationManager.AppSettings["UpdateUserName"] ||
                     password != ConfigurationManager.AppSettings["UpdatePassword"])
            {
                throw new Exception("Invalid user, please check the credentials passed thru URL");
            }

            if (String.IsNullOrEmpty(domain))
            {
                domain = ConfigurationManager.AppSettings["domain"];
            }

            if (String.IsNullOrEmpty(record))
            {
                record = ConfigurationManager.AppSettings["record"];
            }

        }

        private ChangeResourceRecordSetsResponse DeleteRecord(String domain,
                                                                      String record,
                                                                      AmazonRoute53Client client,
                                                                      String zoneId,
                                                                      ResourceRecordSet resourceRecordSetTarget)
        {
            ChangeResourceRecordSetsRequest deleteResourceRecordSetsRequest = new ChangeResourceRecordSetsRequest();
            deleteResourceRecordSetsRequest.HostedZoneId = zoneId;
            ChangeBatch deleteBatch = new ChangeBatch();
            Change deleteChange = new Change();
            deleteChange.Action = "DELETE";
            deleteChange.ResourceRecordSet = resourceRecordSetTarget;
            deleteBatch.Changes = new List<Change>() {deleteChange};
            deleteResourceRecordSetsRequest.ChangeBatch = deleteBatch;
            return client.ChangeResourceRecordSets(deleteResourceRecordSetsRequest);
        }

        private ChangeResourceRecordSetsResponse CreateRecord(String domain, String record, AmazonRoute53Client client, String zoneId)
        {
            ChangeResourceRecordSetsRequest createResourceRecordSetsRequest = new ChangeResourceRecordSetsRequest();
            createResourceRecordSetsRequest.HostedZoneId = zoneId;
 
            ResourceRecordSet createResourceRecordSet = new ResourceRecordSet();
            createResourceRecordSet.Type = "A";
            createResourceRecordSet.TTL = long.Parse(ConfigurationManager.AppSettings["TTL"]);
            createResourceRecordSet.Name = record + "." + domain + ".";
            createResourceRecordSet.ResourceRecords.Add(new ResourceRecord() {Value = IPV4Helper.GetIP4Address()});

            Change createChange = new Change();
            createChange.Action = "CREATE";
            createChange.ResourceRecordSet = createResourceRecordSet;
            
            ChangeBatch createBatch = new ChangeBatch();
            createBatch.Changes = new List<Change>() {createChange};
            createResourceRecordSetsRequest.ChangeBatch = createBatch;

            return client.ChangeResourceRecordSets(createResourceRecordSetsRequest);
        }

        private ResourceRecordSet GetTargetResourceRecordsSet(String domain, String record, AmazonRoute53Client client,
                                                              String zoneId)
        {
            ListResourceRecordSetsRequest listResourceRecordSetsRequest = new ListResourceRecordSetsRequest();
            listResourceRecordSetsRequest.HostedZoneId = zoneId;

            ListResourceRecordSetsResponse recordSetsResponse =
                client.ListResourceRecordSets(listResourceRecordSetsRequest);

            return (from resourceRecordSet in recordSetsResponse.ListResourceRecordSetsResult.ResourceRecordSets
                    where ((resourceRecordSet.Name == record + "." + domain + ".") && (resourceRecordSet.Type == "A"))
                    select resourceRecordSet).FirstOrDefault();
        }

        private Boolean PollResourceRecordChange(String changeId, AmazonRoute53Client client)
        {
        GetChangeRequest  pollRequest = new GetChangeRequest ();
            pollRequest.Id = changeId;
            GetChangeResponse pollResponce =  client.GetChange(pollRequest);
            return pollResponce.GetChangeResult.ChangeInfo.Status == "INSYNC";
        }

        private void WaitForChange(String changeId, AmazonRoute53Client client)
        {

            int timer = 0;
            while (!PollResourceRecordChange(changeId, client))
            {
                if (timer > 60)
                {
                    throw new TimeoutException("Something wrong went wrong at AWS Route53 side. Please check your zone and cleanup it if necessary.");
                }
                System.Threading.Thread.Sleep(1000);
                timer++;
            }
        }

        public ActionResult InfoPage()
        {
            return View();
        }
    }

}




