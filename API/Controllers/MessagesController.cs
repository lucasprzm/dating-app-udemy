using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using API.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using API.Helpers;

namespace API.Controllers
{
  public class MessagesController : BaseApiController
  {
    public readonly IUnitOfWork _uow;
    public readonly IMapper _mapper;
    public MessagesController(IUnitOfWork uow, IMapper mapper)
    {
      _mapper = mapper;
      _uow = uow;
    }

    [HttpPost]
    public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
    {
      var username = User.GetUsername();

      if(username == createMessageDto.RecipientUsername.ToLower())
        return BadRequest("You cannot send messages to yourself");

      var sender = await _uow.UserRepository.GetUserByUsernameAsync(username);
      var recipient = await _uow.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

      if(recipient == null) return NotFound();

      var message = new Message
      {
        Sender = sender,
        Recipient = recipient,
        RecipientUsername = recipient.UserName,
        Content = createMessageDto.Content,
        SenderUsername = sender.UserName
      };

      _uow.MessageRepository.AddMessage(message);

      if(await _uow.Complete()) return Ok(_mapper.Map<MessageDto>(message));

      return BadRequest("Failed to send message");
    }

    [HttpGet]
    public async Task<ActionResult<PagedList<MessageDto>>> GetMessagesForUser([FromQuery]MessageParams messageParams)
    {
      messageParams.Username = User.GetUsername();

      var messages = await _uow.MessageRepository.GetMessagesForUser(messageParams);

      Response.AddPaginationHeader(new PaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages));

      return messages;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMessage(int id)
    {
      var username = User.GetUsername();

      var message = await _uow.MessageRepository.GetMessage(id);

      if(message.SenderUsername != username && message.RecipientUsername != username)
        return Unauthorized();

      if(message.SenderUsername == username)
        message.SenderDeleted = true;

      if(message.RecipientUsername == username)
        message.RecipientDeleted = true;

      if(message.SenderDeleted && message.RecipientDeleted)
      {
        _uow.MessageRepository.DeleteMessage(message);
      }

      if(await _uow.Complete())
        return Ok();

        return BadRequest("Problem deleting the message.");
    }
  }
}