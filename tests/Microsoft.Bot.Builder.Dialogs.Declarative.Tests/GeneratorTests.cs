﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Bot.Builder.MockLuis;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Bot.Builder.Dialogs.Declarative.Tests
{
    public class GeneratorTests : IClassFixture<GeneratorTests.FormFixture>
    {
        private FormFixture _form;

        private readonly string resourcesDirectory = PathUtils.NormalizePath(@"..\..\..\..\..\tests\Microsoft.Bot.Builder.Dialogs.Declarative.Tests\generated\");

        public GeneratorTests(FormFixture form)
        {
            _form = form;
        }

        [Fact]
        public async Task TestSimple()
        {
            await BuildTestFlow("TestAsk", @"sandwich.main.dialog")
                .Send("hi")
                .AssertReply("Welcome!")
                .AssertReply("Which value do you want for bread?")
                .Send("rye")
                .AssertReply("Which value do you want for cheese?")
                .Send("swiss")
                .AssertReply("Which value do you want for meat?")
                .Send("ham")
                .AssertReply("meat=ham, bread=rye, cheese=swiss")
                .AssertReply("Is there any property you want to change? (yes/no)")
                .Send("yes")
                .AssertReply("Which property do you want to change?")
                .Send("bread")
                .AssertReply("Which value do you want for bread?")
                .Send("wheat")
                .AssertReply("Please choose a value for bread from [multi grain wheat, whole wheat]")
                .Send("whole wheat with ham")
                .AssertReply("meat is changed from ham to ham.")
                .AssertReply("meat=ham, bread=whole wheat, cheese=swiss")
                .AssertReply("Is there any property you want to change? (yes/no)")
                .Send("dlkj")
                .AssertReply("Sorry, I do not understand 'dlkj'\n\nEnter any string for confirmation")
                .AssertReply("meat=ham, bread=whole wheat, cheese=swiss")
                .AssertReply("Is there any property you want to change? (yes/no)")
                .Send("sure")
                .AssertReply("Which property do you want to change?")
                .Send("bread")
                .AssertReply("Which value do you want for bread?")
                .Send("none")
                .AssertReply("Did you mean \"none\" as cheese or \"none\" as meat")
                .Send("cheese and roast beef")
                .AssertReply("meat is changed from ham to roast beef.")
                .AssertReply("Which value do you want for bread?")
                .Send("rye")
                .AssertReply("meat=roast beef, bread=rye, cheese=none")
                .AssertReply("Is there any property you want to change? (yes/no)")
                .Send("no")
             .StartTestAsync();
        }

        private TestFlow BuildTestFlow(string testName, string resourceName, bool sendTrace = false)
        {
            TypeFactory.Configuration = new ConfigurationBuilder()
                .UseLuisSettings(resourcesDirectory, "formTests")
                .Build();
            var storage = new MemoryStorage();
            var convoState = new ConversationState(storage);
            var userState = new UserState(storage);
            var adapter = new TestAdapter(TestAdapter.CreateConversation(testName), sendTrace);
            adapter
                .UseStorage(storage)
                .UseState(userState, convoState)
                .UseResourceExplorer(_form.Resources)
                .UseAdaptiveDialogs()
                .UseLanguageGeneration(_form.Resources)
                .UseMockLuis()
                .Use(new TranscriptLoggerMiddleware(new FileTranscriptLogger()));

            var resource = _form.Resources.GetResource(resourceName);
            if (resource == null)
            {
                throw new Exception($"Resource[{resourceName}] not found");
            }

            var dialog = DeclarativeTypeLoader.Load<Dialog>(resource, _form.Resources, DebugSupport.SourceMap);
            var dm = new DialogManager(dialog);

            return new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await dm.OnTurnAsync(turnContext, cancellationToken: cancellationToken).ConfigureAwait(false);
            });
        }

        public class FormFixture : IDisposable
        {
            public FormFixture()
            {
                TypeFactory.Configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
                var projPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, PathUtils.NormalizePath($@"..\..\..\..\..\tests\Microsoft.Bot.Builder.Dialogs.Adaptive.Tests\Microsoft.Bot.Builder.Dialogs.Adaptive.Tests.csproj")));
                Resources = ResourceExplorer.LoadProject(projPath);
            }

            public ResourceExplorer Resources { get; }

            public void Dispose()
            {
                Resources.Dispose();
            }
        }
    }
}