using System.Threading.Tasks;
using kirchnerd.StompNet.Internals.Middleware;
using kirchnerd.StompNet.Internals.Transport.Frames;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace kirchnerd.StompNet.Tests;

[TestClass]
public class PipelineTest
{
    [TestMethod]
    public async Task Given_a_valid_Pipeline_When_Executed_Than_Middleware_And_Handlers_should_be_called_in_correct_order()
    {
        var mock = new Mock<IPipelineBehaviorTest>();
        var seq = 0;
        mock.When(() => seq == 0).Setup(m => m.Middleware1Before()).Callback(() => seq++);
        mock.When(() => seq == 1).Setup(m => m.Middleware2Before()).Callback(() => seq++);
        mock.When(() => seq == 2).Setup(m => m.Handle()).Callback(() => seq++);
        mock.When(() => seq == 3).Setup(m => m.Middleware2After()).Callback(() => seq++);
        mock.When(() => seq == 4).Setup(m => m.Middleware1After()).Callback(() => seq++);

        var verifier = mock.Object;
        var inboxPipeline = new Pipeline<InboxContext>();
        inboxPipeline.Use(async (ctx, next) =>
        {
            verifier.Middleware1Before();
            await next();
            verifier.Middleware1After();
        });

        inboxPipeline.Use(async (ctx, next) =>
        {
            verifier.Middleware2Before();
            await next();
            verifier.Middleware2After();
        });

        inboxPipeline.Run((ctx) =>
        {
            verifier.Handle();
            return Task.CompletedTask;
        });

        await inboxPipeline.ExecuteAsync(new InboxContext(StompFrame.CreateSend()));

        mock.Verify(m => m.Middleware1Before());
        mock.Verify(m => m.Middleware2Before());
        mock.Verify(m => m.Handle());
        mock.Verify(m => m.Middleware2After());
        mock.Verify(m => m.Middleware1After());
    }

    [TestMethod]
    public async Task Given_a_pipeline_with_short_circuit_When_Executed_Than_middleware_should_be_skipped()
    {
        var mock = new Mock<IPipelineBehaviorTest>();
        var seq = 0;
        mock.When(() => seq == 0).Setup(m => m.Middleware1Before()).Callback(() => seq++);

        var verifier = mock.Object;
        var inboxPipeline = new Pipeline<InboxContext>();
        inboxPipeline.Use((ctx, next) =>
        {
            verifier.Middleware1Before();
            return Task.CompletedTask;
        });

        inboxPipeline.Use(async (ctx, next) =>
        {
            verifier.Middleware2Before();
            await next();
            verifier.Middleware2After();
        });

        inboxPipeline.Run((ctx) =>
        {
            verifier.Handle();
            return Task.CompletedTask;
        });

        await inboxPipeline.ExecuteAsync(new InboxContext(StompFrame.CreateSend()));

        mock.Verify(m => m.Middleware1Before());
        mock.VerifyNoOtherCalls();
    }

    public interface IPipelineBehaviorTest
    {
        void Middleware1Before();
        void Middleware1After();
        void Middleware2Before();
        void Middleware2After();
        void Handle();
    }
}