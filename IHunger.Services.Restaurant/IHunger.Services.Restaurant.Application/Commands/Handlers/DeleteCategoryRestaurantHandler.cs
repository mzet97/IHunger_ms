﻿using IHunger.Services.Restaurants.Core.Events;
using IHunger.Services.Restaurants.Core.Exceptions;
using IHunger.Services.Restaurants.Core.Repositories;
using IHunger.Services.Restaurants.Infrastructure.MessageBus;
using MediatR;

namespace IHunger.Services.Restaurants.Application.Commands.Handlers
{
    public class DeleteCategoryRestaurantHandler : IRequestHandler<DeleteCategoryRestaurant, Unit>
    {
        private readonly ICategoryRestaurantRepository _categoryRestaurantRepository;

        private readonly IMessageBusClient _messageBus;

        public DeleteCategoryRestaurantHandler(
            ICategoryRestaurantRepository categoryRestaurantRepository,
            IMessageBusClient messageBus)
        {
            _categoryRestaurantRepository = categoryRestaurantRepository;
            _messageBus = messageBus;
        }

        public async Task<Unit> Handle(DeleteCategoryRestaurant request, CancellationToken cancellationToken)
        {
            var entity = await _categoryRestaurantRepository.GetById(request.Id);

            if(entity == null)
            {
                var noticiation = new NotificationError("Delete CategoryRestaurant has error", "Delete CategoryRestaurant has error");
                var routingKey = noticiation.GetType().Name.ToDashCase();

                _messageBus.Publish(noticiation, routingKey, "noticiation-service");

                throw new NotFoundException("Delete Error");
            }

            await _categoryRestaurantRepository.Remove(entity.Id);

            return Unit.Value;
        }
    }
}
