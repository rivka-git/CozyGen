using AutoMapper;
using DTOs;
using Repository;
using Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services
{
    public class OrderService : IOrderService
    {
        IOrderRepository _r;
        IMapper _mapper;
        private readonly IKafkaProducerService _kafkaProducerService;

        public OrderService(IOrderRepository i, IMapper mapperr, IKafkaProducerService kafkaProducerService)
        {
            _mapper = mapperr;
            _r = i;
            _kafkaProducerService = kafkaProducerService;
        }
        public async Task<List<DtoOrder_Id_UserId_Date_Sum_OrderItems?>> GetOrdersUser(int id)
        {

           var o = await _r.GetOrdersUser(id);
           var r = _mapper.Map<List<Order>, List<DtoOrder_Id_UserId_Date_Sum_OrderItems>>(o);
           return r;
        }
        public async Task<DtoOrder_Id_UserId_Date_Sum_OrderItems?> GetOrderById(int id)
        {
            Order o = await _r.GetOrderById(id);
            return _mapper.Map<Order, DtoOrder_Id_UserId_Date_Sum_OrderItems>(o);

        }

        public async Task<DtoOrder_Id_UserId_Date_Sum_OrderItems> AddNewOrder(DtoOrder_Id_UserId_Date_Sum_OrderItems order)
        {
            var ooo = _mapper.Map<DtoOrder_Id_UserId_Date_Sum_OrderItems, Order>(order);
            Order o = await _r.AddNewOrder(ooo);
            var oo = _mapper.Map<Order, DtoOrder_Id_UserId_Date_Sum_OrderItems>(o);

            // Publish an "order created" event. The order is already saved, so a Kafka
            // failure is logged but does not fail the checkout.
            try
            {
                string jsonMessage = JsonSerializer.Serialize(oo);
                await _kafkaProducerService.SendNotificationAsync(oo.OrderId.ToString(), jsonMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Kafka] Failed to publish order event: {ex.Message}");
            }

            return oo;
        }
    }
}