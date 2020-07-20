using System;
using Autodesk.Forge.Bim360.Relationship;
using Microsoft.Extensions.DependencyInjection;
using Sample.Forge;
using Sample.Forge.Coordination;
using Sample.Forge.Data;
using Sample.Forge.Issue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RelationshipSample
{
    class Program
    {
        static void Main(string[] args)
        {
            RunAsync().Wait();
        }

        public static async Task RunAsync()
        {
            var configuration = new SampleConfiguration();

            //-----------------------------------------------------------------------------------------------------
            // Sample Configuration
            //-----------------------------------------------------------------------------------------------------
            // Either add a .adsk-forge/SampleConfiguration.json file to the Environment.SpecialFolder.UserProfile
            // folder (this will be different on windows vs mac/linux) or pass the optional configuration
            // Dictionary<string, string> to set AuthToken, Account and Project values on SampleConfiguration
            //
            // configuration.Configure(new Dictionary<string, string>
            // {
            //     { "AuthToken", "Your Forge App OAuth token" },
            //     { "AccountId", "Your BIM 360 account GUID (no b. prefix)" },
            //     { "ProjectId", "Your BIM 360 project GUID (no b. prefix)"}
            // });
            // 
            // See: SampleConfigurationExtensions.cs for more information.
            //-----------------------------------------------------------------------------------------------------

            configuration.Load();

            // create a service provider for IoC composition
            var serviceProvider = new ServiceCollection()
                .AddSampleForgeServices(configuration)
                .BuildServiceProvider();

            // load the state from GetClashResultsSample
            var fileManager = serviceProvider.GetRequiredService<ILocalFileManager>();
             
            // get the issue container for this project
            var dataClient = serviceProvider.GetRequiredService<IForgeDataClient>();

            //Verifying project id and name
            dynamic obj = await dataClient.GetProjectAsJObject()
                ?? throw new InvalidOperationException($"Could not load prject {configuration.ProjectId}");

            string project_id = obj.data.id;
            string project_name = obj.data.attributes.name;  
            ColourConsole.WriteSuccess($"Verifying project id and name {project_id}, {project_name}");

            //initialize relationship client
            var relationshipClient = serviceProvider.GetRequiredService<IRelationshipClient>();
            Guid project_id_without_b = new Guid(project_id.Replace("b.", ""));

            //search all relationships
            var res_search_without_arg = await relationshipClient.SearchRelationshipsAsync(project_id_without_b, null, null, null, null, null, null, null, null, null, null, null);

            ColourConsole.WriteSuccess($"Search relationships without arguments: count: {res_search_without_arg.Relationships.Count}");
            if (res_search_without_arg.Relationships.Count > 0){
                //dump one relationship data

                ColourConsole.WriteInfo($"One Relationship"); 

                var oneRelationship = res_search_without_arg.Relationships[0];
                printOneRelationship(oneRelationship.Id, oneRelationship.CreatedOn, oneRelationship.IsReadOnly, oneRelationship.IsService, oneRelationship.IsDeleted,
                    oneRelationship.Entities[0].Domain, oneRelationship.Entities[0].Type, oneRelationship.Entities[0].Id,
                    oneRelationship.Entities[1].Domain, oneRelationship.Entities[1].Type, oneRelationship.Entities[1].Id); 
            } 


            //search relationships by createAfter and createdBefore

            var res_search_with_date = await relationshipClient.SearchRelationshipsAsync(project_id_without_b, 
                null, null, null,new DateTime(2020,7,15), new DateTime(2020, 7, 20), null, null, null, null, null, null);

            ColourConsole.WriteSuccess($"Search relationships with Date arguments: count: {res_search_with_date.Relationships.Count}");
            if (res_search_with_date.Relationships.Count > 0)
            {
                //dump one relationship data
                ColourConsole.WriteInfo($"One Relationship");

                var oneRelationship = res_search_with_date.Relationships[0];
                printOneRelationship(oneRelationship.Id, oneRelationship.CreatedOn, oneRelationship.IsReadOnly, oneRelationship.IsService, oneRelationship.IsDeleted,
                    oneRelationship.Entities[0].Domain, oneRelationship.Entities[0].Type, oneRelationship.Entities[0].Id,
                    oneRelationship.Entities[1].Domain, oneRelationship.Entities[1].Type, oneRelationship.Entities[1].Id);
            }


            //search relationships by createAfter and createdBefore and continuationToken
            string one_continuationToken = res_search_without_arg.Page.ContinuationToken;

            var res_search_with_date_continuetoken = await relationshipClient.SearchRelationshipsAsync(project_id_without_b,
                null, null, null, new DateTime(2020, 7, 15), new DateTime(2020, 7, 20), null, null, null, null, null, one_continuationToken);

            ColourConsole.WriteSuccess($"Search relationships with ContinuationToken: count: {res_search_with_date_continuetoken.Relationships.Count}");
            if (res_search_with_date_continuetoken.Relationships.Count > 0)
            {
                //dump one relationship data
                ColourConsole.WriteInfo($"One Relationship");

                var oneRelationship = res_search_with_date_continuetoken.Relationships[0];
                printOneRelationship(oneRelationship.Id, oneRelationship.CreatedOn, oneRelationship.IsReadOnly, oneRelationship.IsService, oneRelationship.IsDeleted,
                     oneRelationship.Entities[0].Domain, oneRelationship.Entities[0].Type, oneRelationship.Entities[0].Id,
                     oneRelationship.Entities[1].Domain, oneRelationship.Entities[1].Type, oneRelationship.Entities[1].Id);
            }



            //sync
            string oneSyncToken = res_search_without_arg.Page.SyncToken;
            RelationshipSyncRequest rsBody = new RelationshipSyncRequest();
            rsBody.SyncToken = oneSyncToken;
            rsBody.Filters = null;
            var res_sync = await relationshipClient.RelationshipsSyncAsync(project_id_without_b, rsBody);
            ColourConsole.WriteSuccess($"Sync relationships with SyncToken:");
            ColourConsole.WriteInfo($"Current.Data Count: {res_sync.Current.Data.Count}");
            ColourConsole.WriteInfo($"Deleted.Data Count: {res_sync.Deleted.Data.Count}");
            ColourConsole.WriteInfo($"moreData : {res_sync.MoreData}");
            ColourConsole.WriteInfo($"overwrite : {res_sync.Overwrite}");

            if (res_sync.Current.Data.Count > 0)
            {
                //dump one relationship data
                ColourConsole.WriteInfo($"One Current.Data: ");

                var oneRelationship = res_sync.Current.Data[0];
                printOneRelationship(oneRelationship.Id, oneRelationship.CreatedOn, oneRelationship.IsReadOnly, oneRelationship.IsService, oneRelationship.IsDeleted,
                    oneRelationship.Entities[0].Domain, oneRelationship.Entities[0].Type, oneRelationship.Entities[0].Id,
                    oneRelationship.Entities[1].Domain, oneRelationship.Entities[1].Type, oneRelationship.Entities[1].Id);
            }


            if (res_sync.Deleted.Data.Count > 0)
            {
                //dump one relationship data
                ColourConsole.WriteInfo($"One Deleted.Data: ");

                var oneRelationship = res_sync.Deleted.Data[0];
                printOneRelationship(oneRelationship.Id, oneRelationship.CreatedOn, oneRelationship.IsReadOnly, oneRelationship.IsService, oneRelationship.IsDeleted,
                     oneRelationship.Entities[0].Domain, oneRelationship.Entities[0].Type, oneRelationship.Entities[0].Id,
                     oneRelationship.Entities[1].Domain, oneRelationship.Entities[1].Type, oneRelationship.Entities[1].Id);
            }




            //search relationships with one domain .e.g. modelcoordination
            var res_search_with_domain = await relationshipClient.SearchRelationshipsAsync(project_id_without_b,
                "autodesk-bim360-modelcoordination", null, null, new DateTime(2020, 7, 15), new DateTime(2020, 7, 20), null, null, null, null, null, one_continuationToken);

            ColourConsole.WriteSuccess($"Search relationships with one domain(e.g. modelcoordination) count: {res_search_with_domain.Relationships.Count}");
            if (res_search_with_domain.Relationships.Count > 0)
            {
                //dump one relationship data
                ColourConsole.WriteInfo($"One Relationship");

                var oneRelationship = res_search_with_domain.Relationships[0];
                printOneRelationship(oneRelationship.Id, oneRelationship.CreatedOn, oneRelationship.IsReadOnly, oneRelationship.IsService, oneRelationship.IsDeleted,
                      oneRelationship.Entities[0].Domain, oneRelationship.Entities[0].Type, oneRelationship.Entities[0].Id,
                      oneRelationship.Entities[1].Domain, oneRelationship.Entities[1].Type, oneRelationship.Entities[1].Id);
            }

            //search relationships with one domain and type .e.g. modelcoordination and clashgroup
            var res_search_with_domain_type = await relationshipClient.SearchRelationshipsAsync(project_id_without_b,
                "autodesk-bim360-modelcoordination", "clashgroup", null, new DateTime(2020, 7, 15), new DateTime(2020, 7, 20), null, null, null, null, null, one_continuationToken);

            ColourConsole.WriteSuccess($"Search relationships with one domain and type (e.g. modelcoordination & clashgroup). count: {res_search_with_domain_type.Relationships.Count}");
            if (res_search_with_domain_type.Relationships.Count > 0)
            {
                //dump one relationship data
                ColourConsole.WriteInfo($"One Relationship");

                var oneRelationship = res_search_with_domain_type.Relationships[0];
                printOneRelationship(oneRelationship.Id, oneRelationship.CreatedOn, oneRelationship.IsReadOnly, oneRelationship.IsService, oneRelationship.IsDeleted,
                     oneRelationship.Entities[0].Domain, oneRelationship.Entities[0].Type, oneRelationship.Entities[0].Id,
                     oneRelationship.Entities[1].Domain, oneRelationship.Entities[1].Type, oneRelationship.Entities[1].Id);
            }


            //search relationships with one domain and type , and withDomain and withType
            //e.g. modelcoordination & clashgroup  - autodesk-bim360-documentmanagement & documentversion
            //
            var res_search_with_withDomain_type = await relationshipClient.SearchRelationshipsAsync(project_id_without_b,
                "autodesk-bim360-modelcoordination", "clashgroup", null, new DateTime(2020, 7, 15), new DateTime(2020, 7, 20), "autodesk-bim360-documentmanagement", "documentversion", null, null, null, one_continuationToken);

            ColourConsole.WriteSuccess($"Search relationships with one domain and type and withDomain and withType" +
                $" (e.g. modelcoordination & clashgroup  - autodesk-bim360-documentmanagement & documentversion). count: {res_search_with_domain_type.Relationships.Count}");
            if (res_search_with_withDomain_type.Relationships.Count > 0)
            {
                //dump one relationship data
                ColourConsole.WriteInfo($"One Relationship");

                var oneRelationship = res_search_with_withDomain_type.Relationships[0];
                printOneRelationship(oneRelationship.Id, oneRelationship.CreatedOn, oneRelationship.IsReadOnly, oneRelationship.IsService, oneRelationship.IsDeleted,
                      oneRelationship.Entities[0].Domain, oneRelationship.Entities[0].Type, oneRelationship.Entities[0].Id,
                      oneRelationship.Entities[1].Domain, oneRelationship.Entities[1].Type, oneRelationship.Entities[1].Id);
            }



            //intersect without withEntities
            IntersectRelationshipsRequest intersectReqBody = new IntersectRelationshipsRequest();
            DomainEntity oneDomainEntity = new DomainEntity();
            oneDomainEntity.Domain = "autodesk-bim360-modelcoordination";
            oneDomainEntity.Type = "scope";
            oneDomainEntity.Id = "2e905b04-5666-4a8d-a303-27807c132900";

            intersectReqBody.Entities = new List<NewDomainEntity>();
            intersectReqBody.Entities.Add(oneDomainEntity); 


            var res_intersect_relationship = await relationshipClient.IntersectRelationshipsAsync(project_id_without_b, false, null, null, intersectReqBody);

            ColourConsole.WriteSuccess($"get intersect relationships [autodesk-bim360-modelcoordination & clashgroup & df0743c0-e1bb-11e9-96ff-ad6fd4c4196b]\n" +
                                                                    $"Count:{res_intersect_relationship.Relationships.Count}");
            if (res_intersect_relationship != null && res_intersect_relationship.Relationships.Count > 0)
            {
                //dump one relationship data
                var oneRelationship = res_intersect_relationship.Relationships[0];

                printOneRelationship(oneRelationship.Id, oneRelationship.CreatedOn, oneRelationship.IsReadOnly, oneRelationship.IsService, oneRelationship.IsDeleted,
                     oneRelationship.Entities[0].Domain, oneRelationship.Entities[0].Type, oneRelationship.Entities[0].Id,
                     oneRelationship.Entities[1].Domain, oneRelationship.Entities[1].Type, oneRelationship.Entities[1].Id);
            }


            //intersect with withEntities 
            PartialDomainEntity oneWithDomainEntity = new PartialDomainEntity();
            oneWithDomainEntity.Domain = "autodesk-bim360-issue";
            oneWithDomainEntity.Type = "coordination";
            oneWithDomainEntity.Id = "23733044-049d-4ee6-8add-8257248e116f";

            intersectReqBody.WithEntities = new List<PartialDomainEntity>(); 
            intersectReqBody.WithEntities.Add(oneWithDomainEntity);


            res_intersect_relationship = await relationshipClient.IntersectRelationshipsAsync(project_id_without_b, false,null,null, intersectReqBody);

            ColourConsole.WriteSuccess($"get intersect relationships [autodesk-bim360-modelcoordination & clashgroup & df0743c0-e1bb-11e9-96ff-ad6fd4c4196b]\n" +
                                                                    $" [autodesk-bim360-issue & coordination & 9b510ffb-d0c5-4c7d-91ba-044bdb0e77f0]\n" +
                                                                    $"Count:{res_intersect_relationship.Relationships.Count}");
            if (res_intersect_relationship!=null && res_intersect_relationship.Relationships.Count>0)
            {
                //dump one relationship data
                var oneRelationship = res_intersect_relationship.Relationships[0];

                printOneRelationship(oneRelationship.Id, oneRelationship.CreatedOn, oneRelationship.IsReadOnly, oneRelationship.IsService, oneRelationship.IsDeleted,
                     oneRelationship.Entities[0].Domain, oneRelationship.Entities[0].Type, oneRelationship.Entities[0].Id,
                     oneRelationship.Entities[1].Domain, oneRelationship.Entities[1].Type, oneRelationship.Entities[1].Id);
            } 


        }

        private static void printOneRelationship(Guid id, DateTimeOffset createdOn, bool isReadOnly,bool isService,bool isDeleted,
                                    string domain,string type,string entId,
                                    string withDomain, string withType, string withEntId)
        {
            ColourConsole.WriteInfo($"One Relationship Data");
            ColourConsole.WriteInfo($"    Relationship Id: {id}");
            ColourConsole.WriteInfo($"    CreatedOn: {createdOn}");
            ColourConsole.WriteInfo($"    isReadOnly: {isReadOnly}");
            ColourConsole.WriteInfo($"    isService: {isService}");
            ColourConsole.WriteInfo($"    isDeleted: {isDeleted}");
            ColourConsole.WriteInfo($"    entities: ");
            ColourConsole.WriteInfo($"       one entitity: domain={domain}\n" +
                                       $"                     type={type}\n" +
                                       $"                     id={entId}\n");
            ColourConsole.WriteInfo($"       another entitity: domain={withDomain}\n" +
                                     $"                           type={withType}\n" +
                                     $"                           id={withEntId}");
        }
    }



}


