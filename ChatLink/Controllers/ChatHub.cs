using System.Collections.Concurrent;
using System.Text;
using ChatLink.Models.DTOs;
using ChatLink.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace ChatLink.Controllers;

[Authorize]
public class ChatHub : Hub
{
    private const int maxSizeInBytes = 32768;

    private readonly IChatService _chatService;
    private static ConcurrentDictionary<string, List<string>> connections = new();
    private static ConcurrentDictionary<string, string> chatReadyForCreation = new();

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public override async Task OnConnectedAsync()
    {
        var context = Context.GetHttpContext();

        if (context is null)
        {
            return;
        }

        string email = GetCurrentClientId();

        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("Can't find User Id");
            return;
        }

        Console.WriteLine("connection id: " + Context.ConnectionId);
        Console.WriteLine("id: " + email);


        AddOrUpdate(email, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string s = $"{Context.ConnectionId} has been disconnected. ";

        var context = Context.GetHttpContext();

        if (context is null)
        {
            return;
        }

        string email = GetCurrentClientId();

        if (string.IsNullOrEmpty(email))
        {
            Console.WriteLine("Can't find User Id");
            return;
        }

        s += email;

        RemoveConnection(email, Context.ConnectionId);

        Console.WriteLine(s);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendPublicKey(string clientId, string publicKey)
    {
        string email = GetCurrentClientId();

        if (!connections.TryGetValue(clientId, out var connectionIds) || string.IsNullOrEmpty(email))
        {
            return;
        }

        await Clients.Clients(connectionIds).SendAsync("ReceivePublicKey", email, publicKey);
    }

    public async Task CreateChat(string clientId)
    {
        string email = GetCurrentClientId();
        connections.TryGetValue(clientId, out var connectionIds1);
        connections.TryGetValue(email, out var connectionIds2);

        if (connectionIds1.IsNullOrEmpty() || connectionIds2.IsNullOrEmpty() || string.IsNullOrEmpty(email))
        {
            return;
        }

        Console.WriteLine($"create chat user1: {email}, user2: {clientId}");

        chatReadyForCreation.AddOrUpdate(email, clientId, (x, y) => clientId);

        if (chatReadyForCreation.TryGetValue(clientId, out var _))
        {
            await _chatService.CreateChat(clientId, email);
            chatReadyForCreation.Clear();

            foreach (var connectionId1 in connectionIds1)
            {
                await Clients.Client(connectionId1).SendAsync("UpdateContacts");
                await Clients.Client(connectionId1).SendAsync("UpdateChats");
            }

            foreach (var connectionId2 in connectionIds2)
            {
                await Clients.Client(connectionId2).SendAsync("UpdateContacts");
                await Clients.Client(connectionId2).SendAsync("UpdateChats");
            }
        }
    }

    public async Task SendMessage(string clientId, MessageTinyDto messageTinyDto)
    {
        Console.WriteLine(messageTinyDto.EncryptedMessage);

        string email = GetCurrentClientId();
        if (string.IsNullOrEmpty(email) || messageTinyDto.SessionId == Guid.Empty || string.IsNullOrWhiteSpace(messageTinyDto.EncryptedMessage))
        {
            return;
        }

        var messageId = await _chatService.SaveMessage(email, messageTinyDto);
        if (messageId == null)
        {
            //TODO: add error of sending the message
            return;
        }

        var mess = await _chatService.GetMessageDto(email, messageId.Value);

        if (mess == null)
        {
            //TODO: add error of sending the message
            return;
        }

        string json = JsonConvert.SerializeObject(mess);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

        connections.TryGetValue(clientId, out var connectionIds1);
        connections.TryGetValue(email, out var connectionIds2);

        if (jsonBytes.Length > maxSizeInBytes)
        {
            if (!connectionIds1.IsNullOrEmpty())
            {
                foreach (var connectionId1 in connectionIds1)
                {
                    await Clients.Client(connectionId1).SendAsync("NeedToUpdateChat", email);
                }
            }

            if (!connectionIds2.IsNullOrEmpty())
            {
                foreach (var connectionId2 in connectionIds2)
                {
                    await Clients.Client(connectionId2).SendAsync("NeedToUpdateChat", clientId);
                }
            }
        }
        else 
        {
            if (!connectionIds1.IsNullOrEmpty())
            {
                await Clients.Clients(connectionIds1).SendAsync("UpdateChat", email, json);
            }

            if (!connectionIds2.IsNullOrEmpty())
            {
                await Clients.Clients(connectionIds2).SendAsync("UpdateChat", clientId, json);
            }
        }
    }



    public async Task SendOffer(string clientId, string offer)
    {
        Console.WriteLine($"SendOffer to: {clientId}");

        connections.TryGetValue(clientId, out var connectionIds);

        if (!connectionIds.IsNullOrEmpty())
        {
            string email = GetCurrentClientId();

            await Clients.Clients(connectionIds).SendAsync("ReceiveOffer", email, offer);
        }
    }

    public async Task SendAnswer(string clientId, string answer)
    {
        Console.WriteLine($"SendAnswer to: {clientId}");

        connections.TryGetValue(clientId, out var connectionIds);

        if (!connectionIds.IsNullOrEmpty())
        {
            string email = GetCurrentClientId();

            await Clients.Clients(connectionIds).SendAsync("ReceiveAnswer", email, answer);
        }
    }

    public async Task SendIceCandidate(string clientId, string candidate)
    {
        Console.WriteLine($"SendIceCandidate to: {clientId}");

        connections.TryGetValue(clientId, out var connectionIds);

        if (!connectionIds.IsNullOrEmpty())
        {
            string email = GetCurrentClientId();

            await Clients.Clients(connectionIds).SendAsync("ReceiveIceCandidate", email, candidate);
        }
    }

    public async Task CallToUser(string clientId)
    {
        Console.WriteLine($"CallToUser to: {clientId}");

        connections.TryGetValue(clientId, out var connectionIds);

        if (!connectionIds.IsNullOrEmpty())
        {
            string email = GetCurrentClientId();

            await Clients.Clients(connectionIds).SendAsync("ReceiveTheCall", email);
        }
    }

    public async Task AccepttheCall(string clientId)
    {
        Console.WriteLine($"AccepttheCall to: {clientId}");

        connections.TryGetValue(clientId, out var connectionIds);

        if (!connectionIds.IsNullOrEmpty())
        {
            string email = GetCurrentClientId();

            await Clients.Clients(connectionIds).SendAsync("AccepttheCall", email);
        }
    }

    public async Task RejecttheCall(string clientId)
    {
        Console.WriteLine($"RejecttheCall to: {clientId}");

        connections.TryGetValue(clientId, out var connectionIds);

        if (!connectionIds.IsNullOrEmpty())
        {
            string email = GetCurrentClientId();

            await Clients.Clients(connectionIds).SendAsync("RejecttheCall", email);
        }
    }

    public async Task FinishCall(string clientId)
    {
        Console.WriteLine($"FinishCall to: {clientId}");

        connections.TryGetValue(clientId, out var connectionIds);

        if (!connectionIds.IsNullOrEmpty())
        {
            await Clients.Clients(connectionIds).SendAsync("FinishCall");
        }

        string email = GetCurrentClientId();
        connections.TryGetValue(email, out var connectionIds2);

        if (!connectionIds2.IsNullOrEmpty())
        {
            await Clients.Clients(connectionIds2).SendAsync("FinishCall");
        }
    }

    public static void AddOrUpdate(string key, string connection)
    {
        connections.AddOrUpdate(key, [connection], (existingKey, existingList) =>
            {
                lock (existingList)
                {
                    existingList.Add(connection);
                }

                return existingList;
            });
    }

    public static void RemoveConnection(string key, string connection)
    {
        if (connections.TryGetValue(key, out var connectionList))
        {
            lock (connectionList)
            {
                connectionList.Remove(connection);
                if (connectionList.Count == 0)
                {
                    connections.TryRemove(key, out _);
                }
            }
        }
    }

    private string GetCurrentClientId()
    {
        var context = Context.GetHttpContext();

        if (context is null)
        {
            return string.Empty;
        }

        string? email = context.User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value;

        if (email == null)
        {
            Console.WriteLine("Can't find User Id");

            return string.Empty;
        }

        return email;
    }
}
