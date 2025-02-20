﻿using FakeXrmEasy.Abstractions.Metadata;
using FakeXrmEasy.FakeMessageExecutors;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;
using Xunit;

namespace FakeXrmEasy.Messages.Tests.FakeMessageExecutors.RetrieveOptionSetRequestTests
{
    public class RetrieveOptionSetRequestTests : FakeXrmEasyTestsBase
    {
        [Fact]
        public void When_can_execute_is_called_with_an_invalid_request_result_is_false()
        {
            var executor = new RetrieveOptionSetRequestExecutor();
            var anotherRequest = new RetrieveMultipleRequest();
            Assert.False(executor.CanExecute(anotherRequest));
        }

        [Fact]
        public void When_execute_is_called_return_Option_Set()
        {
            var optionSet = new Microsoft.Xrm.Sdk.Metadata.OptionSetMetadata { Name = "test" };
            _context.GetProperty<IOptionSetMetadataRepository>().Set("test", optionSet);
            var executor = new RetrieveOptionSetRequestExecutor();

            var req = new RetrieveOptionSetRequest { Name = "test" };

            var response = ((RetrieveOptionSetResponse)executor.Execute(req, _context));

            Assert.Equal(optionSet.Name, ((Microsoft.Xrm.Sdk.Metadata.OptionSetMetadata)response.OptionSetMetadata).Name);
        }

        [Fact]
        public void When_execute_is_called_And_The_request_does_not_have_a_name_throw_servicefault()
        {
            var executor = new RetrieveOptionSetRequestExecutor();
            var req = new RetrieveOptionSetRequest();

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(req, _context));
        }

        [Fact]
        public void When_execute_is_called_And_The_OptionSetMetadata_Does_Not_Exist_In_The_Context_Throw_ServiceFault()
        {
            var executor = new RetrieveOptionSetRequestExecutor();
            var req = new RetrieveOptionSetRequest { Name = "test" };

            Assert.Throws<FaultException<OrganizationServiceFault>>(() => executor.Execute(req, _context));
        }
    }
}