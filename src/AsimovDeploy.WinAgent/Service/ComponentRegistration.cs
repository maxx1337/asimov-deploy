﻿using System;
using System.Linq;
using AsimovDeploy.WinAgent.Framework.Common;
using AsimovDeploy.WinAgent.Framework.Configuration;
using AsimovDeploy.WinAgent.Framework.Models;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.Graph;
using StructureMap.TypeRules;
using log4net;

namespace AsimovDeploy.WinAgent.Service
{
    public static class ComponentRegistration
    {
         private static readonly ILog Log = LogManager.GetLogger(typeof (ComponentRegistration));

         public static void RegisterComponents()
         {
             Log.Debug("Registering components...");

             ObjectFactory.Initialize(registry =>
             {
                // ReSharper disable ConvertToLambdaExpression
                registry.Scan(assembly =>
                {
                    assembly.TheCallingAssembly();
                    assembly.WithDefaultConventions();
                    assembly.ConnectImplementationsToTypesClosing(typeof (IListenTo<>));
                });

                registry.Scan(assembly =>
                {
                    assembly.TheCallingAssembly();
                    assembly.With(new SingletonConvention<IStartable>());
                });

                registry.For<ITaskExecutor>().UseSpecial(x => x.ConstructedBy(y => (TaskExecutor)y.GetInstance<IStartable>((typeof(TaskExecutor).Name))));
                registry.For<INodeFrontPublisher>().UseSpecial(x => x.ConstructedBy(y => (NodeFrontPublisherPublisher)y.GetInstance<IStartable>((typeof(NodeFrontPublisherPublisher).Name))));

             });
         }

        public static void ReadAndRegisterConfiguration()
        {
            var config = new ConfigurationReader().Read("ConfigFiles", Environment.MachineName);
            ObjectFactory.Configure(x => x.For<IAsimovConfig>().Use(config));
        }

        public static void StartStartableComponenters()
        {
            Log.Debug("Starting startable components...");

            foreach (var startable in ObjectFactory.GetAllInstances<IStartable>())
            {
                Log.DebugFormat("Starting {0}", startable.GetType().Name);
                startable.Start();
            }
        }

        public static void StopAll()
        {
            Log.Debug("Stopping all startable components...");

            foreach (var startable in ObjectFactory.GetAllInstances<IStartable>())
            {
                Log.DebugFormat("Stopping {0}", startable.GetType().Name);
                startable.Stop();
            }
        }
    }

    internal class SingletonConvention<TPluginFamily> : IRegistrationConvention
    {
        public void Process(Type type, Registry registry)
        {
            if (!type.IsConcrete() || !type.CanBeCreated() || !type.AllInterfaces().Contains(typeof(TPluginFamily))) return;

            registry.For(typeof(TPluginFamily)).Singleton().Use(type).Named(type.Name);
        }
    }
}