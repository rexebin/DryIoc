﻿using NUnit.Framework;

namespace DryIoc.UnitTests
{
    [TestFixture]
    public class ConstructionTests
    {
        [Test]
        public void Can_use_static_method_for_service_creation()
        {
            var container = new Container();
            container.Register<SomeService>(made: Made.Of(
                r => FactoryMethod.Of(r.ImplementationType.GetMethodOrNull("Create"))));

            var service = container.Resolve<SomeService>();

            Assert.AreEqual("static", service.Message);
        }

        [Test]
        public void Can_use_any_type_static_method_for_service_creation()
        {
            var container = new Container();
            container.Register<IService>(made: Made.Of(typeof(ServiceFactory).GetMethodOrNull("CreateService")));

            var service = container.Resolve<IService>();

            Assert.AreEqual("static", service.Message);
        }

        [Test]
        public void Can_use_any_type_static_method_for_service_creation_Refactoring_friendly()
        {
            var container = new Container();
            container.Register(Made.Of(() => ServiceFactory.CreateService()));

            var service = container.Resolve<IService>();

            Assert.AreEqual("static", service.Message);
        }

        [Test]
        public void Can_register_service_with_static_method_creating_implementation()
        {
            var container = new Container();
            container.Register<IA, A>(Made.Of(() => F.CreateA()));

            var a = container.Resolve<IA>();

            Assert.IsNotNull(a);
        }

        interface IA { }
        class A : IA {}

        static class F
        {
            public static A CreateA()
            {
                return new A();
            }  
        }

        [Test]
        public void Can_use_instance_method_for_service_creation()
        {
            var container = new Container();
            container.Register<ServiceFactory>();
            container.Register<IService>(made: Made.Of(
                typeof(ServiceFactory).GetMethodOrNull("Create"), 
                ServiceInfo.Of<ServiceFactory>()));

            var service = container.Resolve<IService>();

            Assert.AreEqual("instance", service.Message);
        }

        [Test]
        public void Can_use_instance_method_with_resolved_parameter()
        {
            var container = new Container();
            container.Register<ServiceFactory>();
            container.RegisterInstance("parameter");
            container.Register<IService>(made: Made.Of(
                typeof(ServiceFactory).GetMethodOrNull("Create", typeof(string)), 
                ServiceInfo.Of<ServiceFactory>()));

            var service = container.Resolve<IService>();

            Assert.AreEqual("parameter", service.Message);
        }

        [Test]
        public void Can_specify_instance_method_without_strings()
        {
            var container = new Container();
            container.Register<ServiceFactory>();
            container.RegisterInstance("parameter");
            container.Register(Made.Of(r => ServiceInfo.Of<ServiceFactory>(), f => f.Create(default(string))));

            var service = container.Resolve<IService>();

            Assert.AreEqual("parameter", service.Message);
        }

        [Test]
        public void Can_get_factory_registered_with_key()
        {
            var container = new Container();

            container.Register<ServiceFactory>(serviceKey: "factory");
            container.RegisterInstance("parameter");

            container.Register(Made.Of(
                r => ServiceInfo.Of<ServiceFactory>(serviceKey: "factory"),
                f => f.Create(default(string))));

            var service = container.Resolve<IService>();

            Assert.AreEqual("parameter", service.Message);
        }

        [Test]
        public void Can_get_factory_registered_with_key_and_specify_factory_method_parameter_with_key()
        {
            var container = new Container();

            container.Register<ServiceFactory>(serviceKey: "factory");
            container.RegisterInstance("XXX", serviceKey: "myKey");

            container.Register(Made.Of(
                r => ServiceInfo.Of<ServiceFactory>(serviceKey: "factory"),
                f => f.Create(Arg.Of<string>("myKey"))));

            var service = container.Resolve<IService>();

            Assert.AreEqual("XXX", service.Message);
        }

        [Test]
        public void Should_throw_if_instance_factory_unresolved()
        {
            var container = new Container();

            container.Register<IService, SomeService>(made: Made.Of(
                r => FactoryMethod.Of(
                    typeof(ServiceFactory).GetMethodOrNull("Create"), 
                    ServiceInfo.Of<ServiceFactory>())));

            var ex = Assert.Throws<ContainerException>(() =>
                container.Resolve<IService>());

            Assert.AreEqual(Error.UnableToResolveUnknownService, ex.Error);
        }

        [Test]
        public void Should_throw_for_instance_method_without_factory()
        {
            var container = new Container();
            container.Register<IService>(made: Made.Of(typeof(ServiceFactory).GetMethodOrNull("Create")));

            var ex = Assert.Throws<ContainerException>(() => 
                container.Resolve<IService>());

            Assert.AreEqual(Error.FactoryObjIsNullInFactoryMethod, ex.Error);
        }

        [Test]
        public void Should_return_null_if_instance_factory_is_not_resolved_on_TryResolve()
        {
            var container = new Container();
            container.Register<IService>(made: Made.Of(
                typeof(ServiceFactory).GetMethodOrNull("Create"), 
                ServiceInfo.Of<ServiceFactory>()));

            var service = container.Resolve<IService>(IfUnresolved.ReturnDefault);

            Assert.IsNull(service);
        }

        [Test]
        public void What_if_factory_method_returned_incompatible_type()
        {
            var container = new Container();

            var ex = Assert.Throws<ContainerException>(() =>
                container.Register<SomeService>(made: Made.Of(typeof(BadFactory).GetMethodOrNull("Create"))));

            Assert.AreEqual(Error.RegisteredFactoryMethodResultTypesIsNotAssignableToImplementationType, ex.Error);
        }

        [Test]
        public void It_is_fine_to_have_static_ctor()
        {
            var container = new Container();
            container.Register<WithStaticCtor>();

            var service = container.Resolve<WithStaticCtor>();

            Assert.IsNotNull(service);
        }

        #region CUT

        internal class WithStaticCtor
        {
            public static int Hey;
            static WithStaticCtor()
            {
                ++Hey;
            }

            public WithStaticCtor()
            {
                --Hey;
            }
        }

        internal interface IService 
        {
            string Message { get; }
        }

        internal class SomeService : IService
        {
            public string Message { get; private set; }

            internal SomeService(string message)
            {
                Message = message;
            }

            public static SomeService Create()
            {
                return new SomeService("static");
            }
        }

        internal class ServiceFactory
        {
            public static IService CreateService()
            {
                return new SomeService("static");
            }

            public IService Create()
            {
                return new SomeService("instance");
            }

            public IService Create(string parameter)
            {
                return new SomeService(parameter);
            }
        }

        internal class PropertyBasedFactory
        {
            public string Message { get; set; }

            public IService Create()
            {
                return new SomeService(Message);
            }
        }

        internal class BadFactory
        {
            public static string Create()
            {
                return "bad";
            }
        }

        internal class Generic<T>
        {
            public T X { get; private set; }

            public Generic(T x)
            {
                X = x;
            }
        }

        #endregion
    }
}