﻿using System;
using System.Collections.Generic;
using NSaga;
using NSubstitute;
using Xunit;

namespace Tests.PipelineHook
{
    [Collection("InMemorySagaRepository")]
    public class SagaMediatorPipelineTests
    {
        private readonly InMemorySagaRepository repository;
        private readonly SagaMediator sut;
        private readonly IPipelineHook pipelineHook;

        public SagaMediatorPipelineTests()
        {
            var container = new TinyIoCContainer();
            container.RegisterSagas(typeof(SagaMediatorPipelineTests).Assembly);
            var serviceLocator = new TinyIocSagaFactory(container);


            repository = new InMemorySagaRepository(new JsonNetSerialiser(), serviceLocator);
            pipelineHook = Substitute.For<IPipelineHook>();
            var pipelineHooks = new IPipelineHook[] { pipelineHook };

            sut = new SagaMediator(repository, serviceLocator, pipelineHooks);
        }

        [Fact]
        public void Initiation_PipelineHooks_ExecutedInOrder()
        {
            //Arrange
            var initiatingMessage = new MySagaInitiatingMessage(Guid.NewGuid());

            // Act
            sut.Consume(initiatingMessage);

            // Assert
            Received.InOrder(() =>
            {
                pipelineHook.BeforeInitialisation(Arg.Any<PipelineContext>());
                pipelineHook.AfterInitialisation(Arg.Any<PipelineContext>());
                pipelineHook.AfterSave(Arg.Any<PipelineContext>());
            });
        }


        [Fact]
        public void Initiation_ValidationFails_SaveNotCalled()
        {
            //Arrange
            var initiatingMessage = new InitiatingSagaWithErrors(Guid.NewGuid());

            // Act
            sut.Consume(initiatingMessage);

            // Assert
            Received.InOrder(() =>
            {
                pipelineHook.BeforeInitialisation(Arg.Any<PipelineContext>());
                pipelineHook.AfterInitialisation(Arg.Any<PipelineContext>());
            });
            pipelineHook.DidNotReceive().AfterSave(Arg.Any<PipelineContext>());
        }


        [Fact]
        public void Consumed_PipelineHooks_ExecutedInOrder()
        {
            //Arrange
            var correlationId = Guid.NewGuid();
            repository.Save(new MySaga() { CorrelationId = correlationId });

            var message = new MySagaConsumingMessage(correlationId);

            // Act
            sut.Consume(message);

            // Assert
            Received.InOrder(() =>
            {
                pipelineHook.BeforeConsuming(Arg.Any<PipelineContext>());
                pipelineHook.AfterConsuming(Arg.Any<PipelineContext>());
                pipelineHook.AfterSave(Arg.Any<PipelineContext>());
            });
        }


        [Fact]
        public void Consumed_MessageWithErrors_AfterSaveWasNotCalled()
        {
            //Arrange
            var correlationId = Guid.NewGuid();
            repository.Save(new SagaWithErrors() { CorrelationId = correlationId, SagaData = new SagaWithErrorsData(), Headers = new Dictionary<string, string>() });

            var message = new GetSomeConsumedErrorsForSagaWithErrors(correlationId);

            // Act
            sut.Consume(message);

            // Assert
            Received.InOrder(() =>
            {
                pipelineHook.BeforeConsuming(Arg.Any<PipelineContext>());
                pipelineHook.AfterConsuming(Arg.Any<PipelineContext>());
            });
            pipelineHook.DidNotReceive().AfterSave(Arg.Any<PipelineContext>());
        }
    }
}
