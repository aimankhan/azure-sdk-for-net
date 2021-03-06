﻿//  
//  
// Copyright (c) Microsoft.  All rights reserved.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

namespace Relay.Tests.ScenarioTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Management.Relay;
    using Microsoft.Azure.Management.Relay.Models;
    using Microsoft.Rest.ClientRuntime.Azure.TestFramework;
    using Relay.Tests.TestHelper;
    using Xunit;
    public partial class ScenarioTests 
    {
        [Fact]
        public void HybridConnectionsCreateGetUpdateDelete()
        {
            using (MockContext context = MockContext.Start(this.GetType().FullName))
            {
                InitializeClients(context);
                var location = this.ResourceManagementClient.GetLocationFromProvider();

                var resourceGroup = this.ResourceManagementClient.TryGetResourceGroup(location);
                if (string.IsNullOrWhiteSpace(resourceGroup))
                {
                    resourceGroup = TestUtilities.GenerateName(RelayManagementHelper.ResourceGroupPrefix);
                    this.ResourceManagementClient.TryRegisterResourceGroup(location, resourceGroup);
                }

                // Create Namespace
                var namespaceName = TestUtilities.GenerateName(RelayManagementHelper.NamespacePrefix);

                var responseOperationlist = this.RelayManagementClient.Operations.List();

                var responseCheckNameAvailability = this.RelayManagementClient.Namespaces.CheckNameAvailabilityMethod(new CheckNameAvailability { Name = namespaceName });

                var createNamespaceResponse = this.RelayManagementClient.Namespaces.CreateOrUpdate(resourceGroup, namespaceName,
                    new RelayNamespace()
                    {
                        Location = location,
                        Tags = new Dictionary<string, string>()
                        {
                            {"tag1", "value1"},
                            {"tag2", "value2"}
                        }                       

                    });

                Assert.NotNull(createNamespaceResponse);
                Assert.Equal(createNamespaceResponse.Name, namespaceName);
                Assert.Equal(createNamespaceResponse.Tags.Count, 2);
                Assert.Equal(createNamespaceResponse.Type, "Microsoft.Relay/Namespaces");
                TestUtilities.Wait(TimeSpan.FromSeconds(5));

                // Get the created namespace
                var getNamespaceResponse = RelayManagementClient.Namespaces.Get(resourceGroup, namespaceName);
                if (string.Compare(getNamespaceResponse.ProvisioningState.ToString(), "Succeeded", true) != 0)
                    TestUtilities.Wait(TimeSpan.FromSeconds(5));

                getNamespaceResponse = RelayManagementClient.Namespaces.Get(resourceGroup, namespaceName);
                Assert.NotNull(getNamespaceResponse);
                Assert.Equal("Succeeded", getNamespaceResponse.ProvisioningState.ToString(), StringComparer.CurrentCultureIgnoreCase);                
                Assert.Equal(location, getNamespaceResponse.Location, StringComparer.CurrentCultureIgnoreCase);
                
                // Get all namespaces created within a resourceGroup
                var getAllNamespacesResponse = RelayManagementClient.Namespaces.ListByResourceGroup(resourceGroup);
                Assert.NotNull(getAllNamespacesResponse);
                Assert.True(getAllNamespacesResponse.Count() >= 1);
                Assert.True(getAllNamespacesResponse.Any(ns => ns.Name == namespaceName));
                Assert.True(getAllNamespacesResponse.All(ns => ns.Id.Contains(resourceGroup)));

                // Get all namespaces created within the subscription irrespective of the resourceGroup
                getAllNamespacesResponse = RelayManagementClient.Namespaces.List();
                Assert.NotNull(getAllNamespacesResponse);
                Assert.True(getAllNamespacesResponse.Count() >= 1);
                Assert.True(getAllNamespacesResponse.Any(ns => ns.Name == namespaceName));

                // Create HybridConnections Relay  - 
                var hybridConnectionsName = TestUtilities.GenerateName(RelayManagementHelper.HybridPrefix);
                var createdWCFRelayResponse = RelayManagementClient.HybridConnections.CreateOrUpdate(resourceGroup, namespaceName, hybridConnectionsName, new HybridConnection()
                {                                       
                    RequiresClientAuthorization = true
                });

                Assert.NotNull(createdWCFRelayResponse);
                Assert.Equal(createdWCFRelayResponse.Name, hybridConnectionsName);
                Assert.True(createdWCFRelayResponse.RequiresClientAuthorization);                

                var getWCFRelaysResponse = RelayManagementClient.HybridConnections.Get(resourceGroup, namespaceName, hybridConnectionsName);

                Assert.NotNull(createdWCFRelayResponse);
                Assert.Equal(createdWCFRelayResponse.Name, hybridConnectionsName);
                Assert.True(createdWCFRelayResponse.RequiresClientAuthorization);

                //Update User Metadata for HybridConnections
                string strUserMetadata = "usermetadata is a placeholder to store user-defined string data for the HybridConnection endpoint.e.g. it can be used to store  descriptive data, such as list of teams and their contact information also user-defined configuration settings can be stored.";
                createdWCFRelayResponse.UserMetadata = strUserMetadata;
                
                var updateWCFRelays = RelayManagementClient.HybridConnections.CreateOrUpdate(resourceGroup, namespaceName, hybridConnectionsName, createdWCFRelayResponse);
                Assert.Equal(createdWCFRelayResponse.Name, hybridConnectionsName);
                Assert.True(createdWCFRelayResponse.RequiresClientAuthorization);
                Assert.Equal(createdWCFRelayResponse.UserMetadata, strUserMetadata);

                //Get List of all Hybridconnections in given NameSpace. 
                var listAllHybridConnectionResponse =  RelayManagementClient.HybridConnections.ListByNamespace(resourceGroup, namespaceName);
                Assert.NotNull(listAllHybridConnectionResponse);
                Assert.True(listAllHybridConnectionResponse.Count() >= 1);
                Assert.True(listAllHybridConnectionResponse.All(hy => hy.Id.Contains(resourceGroup)));
                                
                //Delete the HybridConnections created
                try
                {
                    RelayManagementClient.HybridConnections.Delete(resourceGroup, namespaceName, hybridConnectionsName);
                }
                catch (Exception ex)
                {
                    Assert.True(ex.Message.Contains("NotFound"));
                }

                try
                {
                    // Delete namespace
                    RelayManagementClient.Namespaces.Delete(resourceGroup, namespaceName);
                }
                catch (Exception ex)
                {
                    Assert.True(ex.Message.Contains("NotFound"));
                }
            }
        }
    }
}
