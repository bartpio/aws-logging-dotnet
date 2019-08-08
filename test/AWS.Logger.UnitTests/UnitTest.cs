﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using AWS.Logger.Core;
using AWS.Logger.TestUtils;
using Xunit;

namespace AWS.Logger.UnitTest
{
    public class UnitTest:IClassFixture<TestFixture>
    {
        AmazonCloudWatchLogsClient client;
        TestFixture _testFixure;

        public UnitTest(TestFixture testFixture)
        {
            _testFixure = testFixture;
        }

        [Fact]
        public async Task RegexTest()
        {
            var logGroupName = "RegexTest";
            var logStreamName = "TestMessage";

            Regex invalid_sequence_token_regex = new
            Regex(@"The given sequenceToken is invalid. The next expected sequenceToken is: (\d+)");
            client = new AmazonCloudWatchLogsClient(RegionEndpoint.USWest2);
            await client.CreateLogGroupAsync(new CreateLogGroupRequest
            {
                LogGroupName = logGroupName
            });

            _testFixure.LogGroupNameList.Add(logGroupName);

            await client.CreateLogStreamAsync(new CreateLogStreamRequest
            {
                LogGroupName = logGroupName,
                LogStreamName = logStreamName
            });
            var putlogEventsRequest = new PutLogEventsRequest
            {
                LogGroupName = logGroupName,
                LogStreamName = logStreamName,
                LogEvents = new List<InputLogEvent>
                {
                    new InputLogEvent
                    {
                        Timestamp = DateTime.Now,
                        Message = "Message1"
                    }
                }
            };
            var response = await client.PutLogEventsAsync(putlogEventsRequest);
            try
            {
                putlogEventsRequest.LogEvents = new List<InputLogEvent>
                {
                    new InputLogEvent
                    {
                        Timestamp = DateTime.Now,
                        Message = "Message2"
                    }
                };

                await client.PutLogEventsAsync(putlogEventsRequest);
            }
            catch (InvalidSequenceTokenException ex)
            {
                var regexResult = invalid_sequence_token_regex.Match(ex.Message);

                if (regexResult.Success)
                {
                    Assert.Equal(regexResult.Groups[1].Value, response.NextSequenceToken);
                }
            }
        }

        [Fact]
        public async Task CoreTest()
        {
            var logGroupName = "CoreTest";
            var logStreamName = "TestMessage";

            client = new AmazonCloudWatchLogsClient(RegionEndpoint.USWest2);
            await client.CreateLogGroupAsync(new CreateLogGroupRequest
            {
                LogGroupName = logGroupName
            });

            _testFixure.LogGroupNameList.Add(logGroupName);

            var config = new AWSLoggerConfig(logGroupName + "X")
            {
                Region = RegionEndpoint.USWest2.SystemName,
                DontCreateLogGroup = true
            };
            var core = new AWSLoggerCore(config, "unit");
            core.LogLibraryAlert += Core_LogLibraryAlert;
            core.AddMessage("tst");
            core.Flush();
            //core.StartMonitor
            // "AWS.Logger.UnitTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=47d62aa78bdedb31"
        }

        private void Core_LogLibraryAlert(object sender, AWSLoggerCore.LogLibraryEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
