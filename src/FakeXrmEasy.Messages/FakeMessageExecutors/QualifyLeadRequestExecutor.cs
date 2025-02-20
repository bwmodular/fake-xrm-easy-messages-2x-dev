﻿using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using FakeXrmEasy.Abstractions.FakeMessageExecutors;
using FakeXrmEasy.Abstractions;

namespace FakeXrmEasy.FakeMessageExecutors
{
    public class QualifyLeadRequestExecutor : IFakeMessageExecutor
    {
        /// <summary>
        /// Determines if the given request can be executed by this executor
        /// </summary>
        /// <param name="request">The OrganizationRequest that is currently executing</param>
        /// <returns></returns>
        public bool CanExecute(OrganizationRequest request)
        {
            return request is QualifyLeadRequest;
        }

        /// <summary>
        /// Implements the execution of the current request with this executor against a particular XrmFakedContext
        /// </summary>
        /// <param name="request">The current request that is being executed</param>
        /// <param name="ctx">The instance of an XrmFakedContext that the request will be executed against</param>
        /// <returns>QualifyLeadResponse</returns>
        /// <exception cref="Exception"></exception>
        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var req = request as QualifyLeadRequest;

            var orgService = ctx.GetOrganizationService();

            if (req.LeadId == null) throw new Exception("Lead Id must be set in request.");

            var leads = (from l in ctx.CreateQuery("lead")
                         where l.Id == req.LeadId.Id
                         select l);

            var leadsCount = leads.Count();

            if (leadsCount != 1) throw new Exception(string.Format("Number of Leads by given LeadId should be 1. Instead it is {0}.", leadsCount));

            // Made here to get access to CreatedEntities collection
            var response = new QualifyLeadResponse();
            response["CreatedEntities"] = new EntityReferenceCollection();

            // Create Account
            if (req.CreateAccount) // ParentAccount
            {
                var account = new Entity("account")
                {
                    Id = Guid.NewGuid()
                };
                account.Attributes["originatingleadid"] = req.LeadId;
                orgService.Create(account);
                response.CreatedEntities.Add(account.ToEntityReference());
            }

            // Create Contact
            if (req.CreateContact)
            {
                var contact = new Entity("contact")
                {
                    Id = Guid.NewGuid()
                };
                contact.Attributes["originatingleadid"] = req.LeadId;
                orgService.Create(contact);
                response.CreatedEntities.Add(contact.ToEntityReference());
            }

            // Create Opportunity
            if (req.CreateOpportunity)
            {
                var opportunity = new Entity("opportunity")
                {
                    Id = Guid.NewGuid()
                };

                // Set OpportunityCurrencyId if given
                // MSDN link:
                // https://msdn.microsoft.com/en-us/library/microsoft.crm.sdk.messages.qualifyleadrequest.opportunitycurrencyid.aspx
                if (req.OpportunityCurrencyId != null)
                {
                    opportunity.Attributes["transactioncurrencyid"] = req.OpportunityCurrencyId;
                }

                // Associate Account or Contact with Opportunity
                // MSDN link:
                // https://msdn.microsoft.com/en-us/library/microsoft.crm.sdk.messages.qualifyleadrequest.opportunitycustomerid.aspx
                if (req.OpportunityCustomerId != null)
                {
                    var logicalName = req.OpportunityCustomerId.LogicalName;

                    // Associate Account or Contact
                    if (logicalName.Equals("account") || logicalName.Equals("contact"))
                    {
                        opportunity.Attributes["customerid"] = req.OpportunityCustomerId;
                    }
                    // Wrong Entity was given as parameter
                    else
                    {
                        throw new Exception(string.Format("Opportunity Customer Id should be connected with Account or Contact. Instead OpportunityCustomerId was given with Entity.LogicalName = {0}", logicalName));
                    }
                }

                opportunity.Attributes["originatingleadid"] = req.LeadId;
                orgService.Create(opportunity);
                response.CreatedEntities.Add(opportunity.ToEntityReference());
            }

            // Actual Lead
            var lead = leads.First();
            lead.Attributes["statuscode"] = new OptionSetValue(req.Status.Value);
            orgService.Update(lead);

            return response;
        }

        /// <summary>
        /// Returns the type of the concrete OrganizationRequest that this executor implements
        /// </summary>
        /// <returns></returns>
        public Type GetResponsibleRequestType()
        {
            return typeof(QualifyLeadRequest);
        }
    }
}