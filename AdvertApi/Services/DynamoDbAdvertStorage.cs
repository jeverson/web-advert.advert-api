using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdvertApi.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AutoMapper;

namespace AdvertApi.Services
{
    public class DynamoDbAdvertStorage : IAdvertStorageService
    {
        private readonly IMapper _mapper;
        private readonly IAmazonDynamoDB _client;

        public DynamoDbAdvertStorage(IMapper mapper, IAmazonDynamoDB client)
        {
            _mapper = mapper;
            _client = client;
        }

        public async Task<string> Add(AdvertModel model)
        {
            var dbModel = _mapper.Map<AdvertDbModel>(model);
            dbModel.Id = new Guid().ToString();
            dbModel.CreationDateTime = DateTime.UtcNow;
            dbModel.Status = AdvertStatus.Pending;

            using (var client = new AmazonDynamoDBClient())
                using (var ctx = new DynamoDBContext(client))
                    await ctx.SaveAsync(dbModel);

            return dbModel.Id;
        }

        public async Task Confirm(ConfirmAdvertModel model)
        {
            using (var ctx = new DynamoDBContext(_client))
            {
                var record = await ctx.LoadAsync<AdvertDbModel>(model.Id);
                if (record == null)
                    throw new KeyNotFoundException($"A record with Id={model.Id} was not found.");

                if (model.Status == AdvertStatus.Active)
                    await UpdateAdvertAsActive(ctx, record);
                else
                    await DeleteAdvert(ctx, record);
            }
        }

        private static async Task DeleteAdvert(DynamoDBContext ctx, AdvertDbModel record)
        {
            await ctx.DeleteAsync(record);
        }

        private static async Task UpdateAdvertAsActive(DynamoDBContext ctx, AdvertDbModel record)
        {
            record.Status = AdvertStatus.Active;
            await ctx.SaveAsync(record);
        }

        public async Task<bool> CheckHealthAsync()
        {
            using (var ctx = new DynamoDBContext(_client))
            {
                var tableData = await _client.DescribeTableAsync("Adverts");
                return string.Compare(tableData.Table.TableStatus, "active", true) == 0;
            }
        }

        public async Task<AdvertDbModel> GetById(string id)
        {
            using (var ctx = new DynamoDBContext(_client))
            {
                var record = await ctx.LoadAsync<AdvertDbModel>(id);
                if (record == null)
                    throw new KeyNotFoundException($"A record with Id={id} was not found.");

                return record;
            }
        }
    }
}
