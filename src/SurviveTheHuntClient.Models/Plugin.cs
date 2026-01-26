using CitizenFX.Core;
using SurviveTheHuntClient.Attributes;
using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Models.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SurviveTheHuntClient.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class SthNamedEvent : Attribute
    {
        private string _eventName;
        public string EventName { get => _eventName; }

        public SthNamedEvent(string eventName)
        {
            _eventName = eventName;
        }
    }
}

namespace SurviveTheHuntClient.Models
{
    public class PluginEvents
    {
        protected object _plugin;

        public PluginEvents() { }
        public PluginEvents(object plugin) : this() { }

        public PluginEvents UsePlugin(object plugin)
        {
            _plugin = plugin;
            return this;
        }
    }


    public delegate void TriggerServerEventProxyDelegate(string eventName, params object[] payload);
    public delegate void TriggerEventProxyDelegate(string eventName, params object[] payload);
    public delegate void AddTickableDelegate(ITickable tickable);
    public delegate void RemoveTickableDelegate(ITickable tickable);

    public struct PluginContext
    {
        public readonly TriggerServerEventProxyDelegate TriggerServerEventProxy;
        public readonly TriggerEventProxyDelegate TriggerEventProxy;
        public readonly AddTickableDelegate AddTickable;
        public readonly RemoveTickableDelegate RemoveTickable;
        public readonly EventHandlerDictionary EventHandlers;

        public PluginContext(TriggerEventProxyDelegate triggerEvent, TriggerServerEventProxyDelegate triggerServerEvent, EventHandlerDictionary eventHandlers, List<ITickable> tickables, List<ITickable> tickablesToRemove)
            : this(triggerEvent, triggerServerEvent, eventHandlers, (ITickable tickable) => tickables.Add(tickable), (ITickable tickable) => tickablesToRemove.Add(tickable))
        {
        }

        public PluginContext(TriggerEventProxyDelegate triggerEvent, TriggerServerEventProxyDelegate triggerServerEvent, EventHandlerDictionary eventHandlers, AddTickableDelegate addTickable, RemoveTickableDelegate removeTickable)
        {
            TriggerEventProxy = triggerEvent;
            TriggerServerEventProxy = triggerServerEvent;
            AddTickable = addTickable;
            RemoveTickable = removeTickable;
            EventHandlers = eventHandlers;
        }
    }

    public class PluginInfo
    {
        private readonly string _name;
        private readonly string _description;
        private readonly string _title;

        public string Name { get => _name; }
        public string Description { get => _description; }
        public string Title { get => _title; }

        public PluginInfo(string name, string title = null, string description = null)
        {
            _name = name;
            _description = description ?? "";
            _title = title ?? "";
        }

        public PluginInfo(IPlugin plugin) : this(plugin.Name, string.IsNullOrWhiteSpace(plugin.GameModeTitle) ? plugin.Name : plugin.GameModeTitle, plugin.GameModeDescription)
        {
        }
    }

    abstract public class Plugin<EventHandlers> : IPlugin where EventHandlers : PluginEvents, new()
    {
        protected readonly TriggerServerEventProxyDelegate TriggerServerEventProxy;
        protected readonly TriggerEventProxyDelegate TriggerEventProxy;
        protected readonly AddTickableDelegate AddTickable;
        protected readonly RemoveTickableDelegate RemoveTickable;

        private readonly EventHandlerDictionary _eventHandlers;

        private List<string> _eventNames = new List<string>();

        private readonly EventHandlers _events;

        private readonly string _name;
        public string Name { get => _name; }

        public bool IsActive { get; set; } = false;

        public virtual bool IsGameMode { get => false; }

        public virtual string GameModeTitle { get => null; }
        public virtual string GameModeDescription { get => null; } 

        public Plugin(string name, PluginContext context)
        {
            TriggerEventProxy = context.TriggerEventProxy;
            TriggerServerEventProxy = context.TriggerServerEventProxy;
            _eventHandlers = context.EventHandlers;
            _events = (EventHandlers)new EventHandlers().UsePlugin(this);
            AddTickable = context.AddTickable;
            RemoveTickable = context.RemoveTickable;
            
            _name = name;
        }

        /*{
            _triggerServerEventProxy(eventName, payload);
        }*/

        public virtual void OnHuntStarted(IGameState gameState, IPlayerState playerState)
        {

        }

        public virtual void OnHuntEnded(IGameState gameState, IPlayerState playerState)
        {

        }

        public delegate void PluginEventHandlerDelegate(params object[] args);

        public class PluginEventInvoker
        {
            private MethodInfo _method;
            private EventHandlers _target;

            public PluginEventInvoker(MethodInfo method, EventHandlers target)
            {
                _method = method;
                _target = target;
            }

            public void Invoke(params object[] args)
            {
                Debug.WriteLine($"Invoking {_method.Name} with {args.Length} params");
                _method.Invoke(_target, args);

                //return null;
            }

            public Delegate Delegate
            {
                get
                {
                    return Delegate.CreateDelegate(typeof(PluginEventHandlerDelegate), this, GetType().GetMethod(nameof(Invoke)), true);
                }
            }
        }

        /// <summary>
        /// Called when the resource has started. Make sure to register your events here.
        /// </summary>
        public virtual void OnResourceStarted()
        {
            MethodInfo[] methods = _events.GetType().GetMethods();
            Debug.WriteLine("Registering event handlers");
            foreach(MethodInfo method in methods)
            {
                Debug.WriteLine(method.Name);
                try
                {
                    SthNamedEvent attrib = method.GetCustomAttribute<SthNamedEvent>();
                    if(attrib != null)
                    {
                        Debug.WriteLine($"{nameof(OnResourceStarted)}: Registering {method.Name} as {attrib.EventName}");
                        //_addEventHandler(attrib.EventName, method.Name);
                        _eventHandlers[attrib.EventName] += new PluginEventInvoker(method, _events).Delegate;
                        Debug.WriteLine("Registered!");
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Called when the resource is stopping. In subclasses, make sure to call this base function, or otherwise events won't be unsubscribed properly.
        /// </summary>
        public virtual void OnResourceStopping()
        {
            foreach (string eventName in _eventNames)
            {
                _eventHandlers.Remove(eventName);
            }
        }

        public virtual bool DoesPlayerNeedInvincibility { get { return false; } }

        public virtual void OnClockReceived(int hours, int minutes, int seconds)
        {

        }

        public virtual bool CanPingShow { get { return true; } }

        public virtual SurviveTheHuntShared.Core.Teams.Team? WinningTeamOverride => null;

        public virtual string CustomWastedText => null;
        public virtual bool SkipAddingPlayerNameInObjective => false;

        public virtual LabelledItem[] UICurrentItems => new LabelledItem[0];

        public virtual SurviveTheHuntShared.Utils.Coord[] CarSpawnPointsOverride => null;

        public virtual bool? IsVehicleWeaponAllowed(int vehicleHandle, uint weapon)
        {
            return null;
        }

        public virtual void OnPlayerSpawned()
        {

        }
    }
}
