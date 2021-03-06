﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TypeEdge.Enums;
using TypeEdge.Modules;
using TypeEdge.Modules.Endpoints;
using TypeEdge.Modules.Enums;
using TypeEdge.Modules.Messages;
using TypeEdge.Twins;
using ThermostatApplication;
using ThermostatApplication.Messages;
using ThermostatApplication.Modules;
using ThermostatApplication.Twins;

namespace Modules
{
    public class Orchestrator : EdgeModule, IOrchestrator
    {
        public Output<Temperature> Training { get; set; }
        public Output<Temperature> Detection { get; set; }
        public Output<Temperature> Visualization { get; set; }
        public ModuleTwin<OrchestratorTwin> Twin { get; set; }

        public Orchestrator(ITemperatureSensor proxy)
        {
            proxy.Temperature.Subscribe(this, async signal =>
            {
                var twin = Twin.LastKnownTwin;
                if (twin != null)
                {
                    if (signal.Scale != twin.Scale)
                        if (twin.Scale == TemperatureScale.Celsius)
                            signal.Value = signal.Value * 9 / 5 + 32;

                    List<Task> messages = new List<Task>();
                    foreach (Routing item in Enum.GetValues(typeof(Routing)))
                        if (twin.RoutingMode.HasFlag(item))
                            messages.Add(RouteMessage(signal, item));

                    if (messages.Count > 0)
                        await Task.WhenAll(messages);
                }
                return MessageResult.Ok;
            });

            Twin.Subscribe(async twin =>
            {
                System.Console.WriteLine($"Preprocessor: new routing : { twin.RoutingMode.ToString()}");
                await Twin.ReportAsync(twin);
                return TwinResult.Ok;
            });
        }

        private Task RouteMessage(Temperature signal, Routing mode)
        {
            switch (mode)
            {
                case Routing.Train:
                    return Training.PublishAsync(signal);
                case Routing.Detect:
                    return Detection.PublishAsync(signal);
                case Routing.Visualize:
                    return Visualization.PublishAsync(signal);
            }
            return null;
        }

        public override async Task<ExecutionResult> RunAsync()
        {
            await Twin.GetAsync();
            return ExecutionResult.Ok;
        }
    }
}