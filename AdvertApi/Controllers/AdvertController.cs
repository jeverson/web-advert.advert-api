using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdvertApi.Models;
using AdvertApi.Models.Messages;
using AdvertApi.Services;
using Amazon.SimpleNotificationService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace AdvertApi.Controllers
{
    [ApiController]
    [Route("adverts/v1")]
    public class AdvertController : ControllerBase
    {
        private readonly IAdvertStorageService _advertStorageService;
        private readonly IConfiguration _configuration;
        private readonly IAmazonSimpleNotificationService _notificationService;

        public AdvertController(IAdvertStorageService advertStorageService, IConfiguration configuration, IAmazonSimpleNotificationService notificationService)
        {
            _advertStorageService = advertStorageService;
            _configuration = configuration;
            _notificationService = notificationService;
        }

        [HttpPost]
        [Route("Create")]
        [ProducesResponseType(400)]
        [ProducesResponseType(200, Type = typeof(CreateAdvertResponse))]
        public async Task<IActionResult> Create(AdvertModel model)
        {
            try
            {
                var id = await _advertStorageService.Add(model);
                return StatusCode(201, new CreateAdvertResponse { Id = id });
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPut]
        [Route("Confirm")]
        [ProducesResponseType(404)]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Confirm(ConfirmAdvertModel model)
        {
            try
            {
                await _advertStorageService.Confirm(model);
                await RaiseAdvertConfirmedMessage(model);
                return new OkResult();
            }
            catch (KeyNotFoundException)
            {
                return new NotFoundResult();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        private async Task RaiseAdvertConfirmedMessage(ConfirmAdvertModel model)
        {
            var topicArn = _configuration.GetValue<string>("TopicArn");
            var dbModel = await _advertStorageService.GetById(model.Id);

            var message = new AdvertConfirmedMessage { Id = model.Id, Title = dbModel.Title };
            var messageJson = JsonConvert.SerializeObject(message);
            await _notificationService.PublishAsync(topicArn, messageJson);
        }
    }
}