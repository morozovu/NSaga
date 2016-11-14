﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSaga;
using Xunit;


namespace Tests.Composition
{
    public class InternalContainerWireupTests
    {
        [Theory]
        [InlineData("sagaRepository", typeof(InMemorySagaRepository))]
        [InlineData("sagaFactory", typeof(TinyIocSagaFactory))]
        [InlineData("pipelineHook", typeof(CompositePipelineHook))]
        public void Default_Provides_InMemoryRepository(string propertyName, Type expectedType)
        {
            var sagaMediator = Wireup.UseInternalContainer().ResolveMediator();

            ValidatePrivateProperty(sagaMediator, propertyName, expectedType);
        }

        [Fact]
        public void NullObjects_Can_Be_Resolved()
        {
            var mediator = Wireup.UseInternalContainer()
                                 .UseSagaFactory<NullSagaFactory>()
                                 .UseRepository<NullSagaRepository>()
                                 .ResolveMediator();

            mediator.Should().NotBeNull();
            ValidatePrivateProperty(mediator, "sagaRepository", typeof(NullSagaRepository));
            ValidatePrivateProperty(mediator, "sagaFactory", typeof(NullSagaFactory));
        }

        [Fact]
        public void AddingPipeline_Adds_ToCollection()
        {
            //Arrange
            var mediator = Wireup.UseInternalContainer()
                                 .AddPiplineHook<NullPipelineHook>()
                                 .ResolveMediator();
            var composite = NSagaReflection.GetPrivate(mediator, "pipelineHook");
            var hooks = (List<IPipelineHook>)NSagaReflection.GetPrivate(composite, "hooks");

            // Assert
            hooks.Should().HaveCount(2);
            hooks.FirstOrDefault(h => h.GetType() == typeof(NullPipelineHook)).Should().NotBeNull();
            hooks.FirstOrDefault(h => h.GetType() == typeof(MetadataPipelineHook)).Should().NotBeNull();
        }

        [Fact]
        public void Default_Can_Initialise_Saga()
        {
            //Arrange
            var correlationId = Guid.NewGuid();
            var sagaMediator = Wireup.UseInternalContainer().ResolveMediator();

            // Act
            var result = sagaMediator.Consume(new MySagaInitiatingMessage(correlationId));

            // Assert
            result.IsSuccessful.Should().BeTrue();
        }


        [Fact]
        public void SagaFactory_CanResolve_ByInitiatedInterface()
        {
            var builder = Wireup.UseInternalContainer().RegisterComponents();
            var sagaFactory = builder.Resolve<ISagaFactory>();

            // Act
            var result = sagaFactory.ResolveSagaInititatedBy(new MySagaInitiatingMessage(Guid.NewGuid()));

            // Assert
            result.Should().NotBeNull().And.BeOfType<MySaga>();
        }



        [Fact]
        public void SagaFactory_CanResolve_ByConsumedInterface()
        {
            var builder = Wireup.UseInternalContainer().RegisterComponents();
            var sagaFactory = builder.Resolve<ISagaFactory>();

            // Act
            var result = sagaFactory.ResolveSagaConsumedBy(new MySagaConsumingMessage(Guid.NewGuid()));

            // Assert
            result.Should().NotBeNull().And.BeOfType<MySaga>();
        }

        private static void ValidatePrivateProperty(ISagaMediator sagaMediator, string propertyName, Type expectedType)
        {
            var propertyValue = NSagaReflection.GetPrivate(sagaMediator, propertyName);

            propertyValue.Should().BeOfType(expectedType);
        }
    }
}
