using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PAQTERIA.Server.Hubs;

[Authorize(Policy = "OperationalStaff")]
public sealed class OperationsHub : Hub { }
