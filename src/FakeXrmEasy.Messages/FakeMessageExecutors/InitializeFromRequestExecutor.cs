﻿using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Abstractions.Exceptions;
using FakeXrmEasy.Abstractions.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace FakeXrmEasy.FakeMessageExecutors
{
    /// <summary>
    /// A fake messag executor that implements the InitializeFromRequest message
    /// </summary>
    public class InitializeFromRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines if the given request can be executed by this executor
        /// </summary>
        /// <param name="request">The OrganizationRequest that is currently executing</param>
        /// <returns></returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is InitializeFromRequest;
        }

        /// <summary>
        /// Returns the type of the concrete OrganizationRequest that this executor implements
        /// </summary>
        /// <returns></returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(InitializeFromRequest);
        }

        /// <summary>
        /// Implements the execution of the current request with this executor against a particular XrmFakedContext
        /// </summary>
        /// <param name="request">The current request that is being executed</param>
        /// <param name="ctx">The instance of an XrmFakedContext that the request will be executed against</param>
        /// <returns>InitializeFromResponse</returns>
        /// <exception cref="Exception"></exception>
        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var req = request as InitializeFromRequest;
            if (req == null)
                throw FakeOrganizationServiceFaultFactory.New( "Cannot execute InitializeFromRequest without the request");

            if (req.TargetFieldType != TargetFieldType.All)
                throw UnsupportedExceptionFactory.PartiallyNotImplementedOrganizationRequest(ctx.LicenseContext.Value, req.GetType(), "logic for filtering attributes based on TargetFieldType other than All is missing");

            var service = ctx.GetOrganizationService();
            var fetchXml = string.Format(FetchMappingsByEntity, req.EntityMoniker.LogicalName, req.TargetEntityName);
            var mapping = service.RetrieveMultiple(new FetchExpression(fetchXml));
            var sourceAttributes = mapping.Entities.Select(a => a.GetAttributeValue<AliasedValue>("attributemap.sourceattributename").Value.ToString()).ToArray();
            var columnSet = sourceAttributes.Length == 0 ? new ColumnSet(true) : new ColumnSet(sourceAttributes);
            var source = service.Retrieve(req.EntityMoniker.LogicalName, req.EntityMoniker.Id, columnSet);

            // If we are using proxy types, and the appropriate proxy type is found in 
            // the assembly create an instance of the appropriate class
            // Otherwise return a simple Entity

            Entity entity = ctx.NewEntityRecord(req.TargetEntityName);
            
            if (mapping.Entities.Count > 0)
            {
                foreach (var attr in source.Attributes)
                {
                    var mappingEntity = mapping.Entities.FirstOrDefault(e => e.GetAttributeValue<AliasedValue>("attributemap.sourceattributename").Value.ToString() == attr.Key);
                    if (mappingEntity == null) continue;
                    var targetAttribute = mappingEntity.GetAttributeValue<AliasedValue>("attributemap.targetattributename").Value.ToString();
                    entity[targetAttribute] = attr.Value;

                    var isEntityReference = string.Equals(attr.Key, source.LogicalName + "id", StringComparison.CurrentCultureIgnoreCase);
                    if (isEntityReference)
                    {
                        entity[targetAttribute] = new EntityReference(source.LogicalName, (Guid)attr.Value);
                    }
                    else
                    {
                        entity[targetAttribute] = attr.Value;
                    }
                }
            }

            var response = new InitializeFromResponse
            {
                Results =
                {
                    ["Entity"] = entity
                }
            };

            return response;
        }

        private const string FetchMappingsByEntity = @"<fetch version='1.0' mapping='logical' distinct='false'>
                                                           <entity name='entitymap'>
                                                              <attribute name='sourceentityname'/>
                                                              <attribute name='targetentityname'/>
                                                              <link-entity name='attributemap' alias='attributemap' to='entitymapid' from='entitymapid' link-type='inner'>
                                                                 <attribute name='sourceattributename'/>
                                                                 <attribute name='targetattributename'/>
                                                              </link-entity>
                                                              <filter type='and'>
                                                                 <condition attribute='sourceentityname' operator='eq' value='{0}' />
                                                                 <condition attribute='targetentityname' operator='eq' value='{1}' />
                                                              </filter>
                                                           </entity>
                                                        </fetch>";
    }
}