#r ScriptTools.dll

using System;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Biosero.DataServices.Client;
using Biosero.Orchestrator.WorkflowService;
using ScriptTools;

namespace Acme.Orchestrator.Scripting
{
public class DebugCheckAllQueuedSources
    {
public async Task RunAsync  (DataServicesClient client, WorkflowContext context, CancellationToken cancellationToken)
        {
            var DebugCheckAllQueuedSources = new ScriptTools.Tools();
            await DebugCheckAllQueuedSources.RunAsync("Ronen", client, context, cancellationToken);
        }
    }
}