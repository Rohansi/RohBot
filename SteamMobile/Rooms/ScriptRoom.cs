using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using CSScriptLibrary;
using SteamMobile.Rooms.Script;
using csscript;

namespace SteamMobile.Rooms
{
    public class ScriptRoom : Room
    {
        public class CommandHandler
        {
            public readonly string Type;
            public readonly string Format;
            public readonly Action<CommandTarget, string[]> Handler;

            public CommandHandler(string type, string format, Action<CommandTarget, string[]> handler)
            {
                Type = type;
                Format = format;
                Handler = handler;
            }
        }

        public override string CommandPrefix { get { return "script_"; } }

        public Dictionary<string, CommandHandler> Commands;

        private string _sourceFile;
        private ScriptHost _host;
        private Stopwatch _timer;
        private IScript _script;
        private bool _compiling;

        public ScriptRoom(RoomInfo roomInfo)
            : base(roomInfo)
        {
            Commands = new Dictionary<string, CommandHandler>();

            _sourceFile = roomInfo["Script"];
            _host = new ScriptHost(this);
            _timer = Stopwatch.StartNew();

            Recompile();
        }

        public override void SendLine(HistoryLine line)
        {
            if (_script != null)
            {
                bool cont = true;
                SafeInvoke(() => cont = _script.OnSendLine(line));
                if (!cont)
                    return;
            }

            base.SendLine(line);
        }

        public override void SendHistory(Session session)
        {
            if (_script != null)
            {
                bool cont = true;
                SafeInvoke(() => cont = _script.OnSendHistory(session));
                if (!cont)
                    return;
            }

            base.SendHistory(session);
        }

        public override void Update()
        {
            var delta = (float)_timer.Elapsed.TotalSeconds;
            _timer.Restart();

            if (_script != null)
                SafeInvoke(() => _script.Update(delta));
        }

        public void Recompile()
        {
            if (_compiling)
                return;

            _host.Reset();
            Commands.Clear();
            _script = null;
            _compiling = true;

            Send("Compiling...");

            ThreadPool.QueueUserWorkItem(a =>
            {
                try
                {
                    var type = Compile();
                    if (type == null)
                        return;

                    Send("Done!");

                    SafeInvoke(() =>
                    {
                        _script = (IScript)Activator.CreateInstance(type);
                        _script.Initialize(_host);
                    });
                }
                finally
                {
                    _compiling = false;
                }
            });
        }

        public void SafeInvoke(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                _script = null;
                SendException(e);
            }
        }

        private Type Compile()
        {
            Evaluator evaluator = null;

            try
            {
                evaluator = new Evaluator();
                evaluator.Reset(false);
                evaluator.ReferenceAssemblyOf<Program>();

                // TODO: make a list of these
                evaluator.ReferenceAssemblyByNamespace("System");
                evaluator.ReferenceAssemblyByNamespace("System.Collections.Generic");
                evaluator.ReferenceAssemblyByNamespace("System.Linq");
                evaluator.ReferenceAssemblyByNamespace("System.Text");
                evaluator.ReferenceAssemblyByNamespace("log4net");
                evaluator.ReferenceAssemblyByNamespace("Newtonsoft.Json");
                evaluator.ReferenceAssemblyByNamespace("Npgsql");
                evaluator.ReferenceAssemblyByNamespace("SteamKit2");
                evaluator.ReferenceAssemblyByNamespace("EzSteam");

                evaluator.CompileCode(File.ReadAllText(_sourceFile));
                return evaluator.GetCompiledType("Script");
            }
            catch (CompilerException e)
            {
                if (evaluator != null && evaluator.CompilingResult.HasErrors)
                {
                    var sb = new StringBuilder();

                    foreach (var error in evaluator.CompilingResult.Errors)
                    {
                        sb.Append(error);
                        sb.Append('\n');
                    }

                    Send(sb.ToString());
                    return null;
                }

                SendException(e);
            }
            catch (Exception e)
            {
                SendException(e);
            }

            return null;
        }

        private void SendException(Exception e)
        {
            Send(e.ToString().Replace("\r", ""));
        }
    }
}
