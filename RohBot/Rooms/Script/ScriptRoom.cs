using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CSScriptLibrary;
using csscript;

namespace RohBot.Rooms.Script
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

        public override string CommandPrefix => "script_";

        public Dictionary<string, CommandHandler> Commands;

        private string _sourceFile;
        private ScriptHost _host;
        private Stopwatch _timer;
        private IScript _script;
        private bool _compiling;
        private List<string> _references;
         
        public ScriptRoom(RoomInfo roomInfo)
            : base(roomInfo)
        {
            Commands = new Dictionary<string, CommandHandler>();

            _sourceFile = roomInfo["Script"];
            _host = new ScriptHost(this);
            _timer = Stopwatch.StartNew();

            _references = new List<string>();
            _references.AddRange((roomInfo["References"] ?? "").Split(','));

            if (Util.IsRunningOnMono())
            {
                _references.AddRange(new List<string>
                {
                    "System", "System.Collections.Generic", "System.Linq", "System.Text"
                });
            }

            _references.AddRange(new List<string>
            {
                "log4net", "Newtonsoft.Json", "Npgsql", "SteamKit2", "EzSteam"
            });

            _references = _references.Distinct().ToList();

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

        public override void SendHistory(Connection connection)
        {
            if (_script != null)
            {
                bool cont = true;
                SafeInvoke(() => cont = _script.OnSendHistory(connection));
                if (!cont)
                    return;
            }

            base.SendHistory(connection);
        }

        public override void SendMessage(Connection connection, string message)
        {
            if (_script != null)
            {
                bool cont = true;
                SafeInvoke(() => cont = _script.OnSendMessage(connection, message));
                if (!cont)
                    return;
            }

            base.SendMessage(connection, message);
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

                foreach (var reference in _references)
                {
                    evaluator.ReferenceAssemblyByNamespace(reference);
                }

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
