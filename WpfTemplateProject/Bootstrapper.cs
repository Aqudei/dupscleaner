using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Caliburn.Micro;
using DupFileCleaner.ViewModels;
using Unity;

namespace DupFileCleaner
{
    class Bootstrapper : BootstrapperBase
    {
        private readonly IUnityContainer _container = new UnityContainer();

        public Bootstrapper()
        {
            Initialize();
        }

        protected override void BuildUp(object instance)
        {
            _container.BuildUp(instance);
        }

        protected override void Configure()
        {
            
            _container.RegisterSingleton<IEventAggregator, EventAggregator>();
            _container.RegisterType<IWindowManager, WindowManager>();
            base.Configure();
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _container.ResolveAll(service);
        }

        protected override object GetInstance(Type service, string key)
        {
            return _container.Resolve(service, key);
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<ShellViewModel>();
        }
    }
}
