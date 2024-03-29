﻿using IHunger.Services.Restaurants.Core.Entities;
using IHunger.Services.Restaurants.Core.Events;
using IHunger.Services.Restaurants.Core.Exceptions;
using IHunger.Services.Restaurants.Core.Repositories;
using IHunger.Services.Restaurants.Core.Validations;
using IHunger.Services.Restaurants.Infrastructure.MessageBus;
using MediatR;

namespace IHunger.Services.Restaurants.Application.Commands.Handlers
{
    public class AddRestaurantHandler : IRequestHandler<AddRestaurant, Guid>
    {
        private readonly ICategoryRestaurantRepository _categoryRestaurantRepository;
        private readonly IRestaurantRepository _restaurantRepository;

        private readonly IMessageBusClient _messageBus;

        public AddRestaurantHandler(
            ICategoryRestaurantRepository categoryRestaurantRepository,
            IRestaurantRepository restaurantRepository,
            IMessageBusClient messageBus)
        {
            _categoryRestaurantRepository = categoryRestaurantRepository;
            _restaurantRepository = restaurantRepository;
            _messageBus = messageBus;
        }

        public async Task<Guid> Handle(AddRestaurant request, CancellationToken cancellationToken)
        {
            var categoryRestaurant = await _categoryRestaurantRepository.GetById(request.IdCategoryRestaurant);

            if(categoryRestaurant == null)
            {
                var noticiation = new NotificationError("categoryRestaurant is null", "categoryRestaurant is null");
                var routingKey = noticiation.GetType().Name.ToDashCase();

                _messageBus.Publish(noticiation, routingKey, "noticiation-service");

                throw new ValidationException("Category is null");
            }

            if(request.AddressRestaurant == null)
            {
                var noticiation = new NotificationError("AddressRestaurant is null", "AddressRestaurant is null");
                var routingKey = noticiation.GetType().Name.ToDashCase();

                _messageBus.Publish(noticiation, routingKey, "noticiation-service");

                throw new ValidationException("AddressRestaurant is null");
            }

            var addressRestaurant = new AddressRestaurant(
                Guid.NewGuid(),
                request.AddressRestaurant.Street,
                request.AddressRestaurant.Number,
                request.AddressRestaurant.District,
                request.AddressRestaurant.City,
                request.AddressRestaurant.County,
                request.AddressRestaurant.ZipCode,
                request.AddressRestaurant.Latitude,
                request.AddressRestaurant.Longitude);

            if (!Validator.Validate(new AddressRestaurantValidation(), addressRestaurant))
            {
                var noticiation = new NotificationError("Validate AddressRestaurant has error", "Validate AddressRestaurant has error");
                var routingKey = noticiation.GetType().Name.ToDashCase();

                _messageBus.Publish(noticiation, routingKey, "noticiation-service");

                throw new ValidationException("Validate Error");
            }
                

            var restaurant = Restaurant.Create(
                request.Name,
                request.Description,
                request.Image,
                addressRestaurant.Id,
                addressRestaurant,
                categoryRestaurant);

            if (!Validator.Validate(new RestaurantValidation(), restaurant))
            {
                var noticiation = new NotificationError("Validate Restaurant has error", "Validate Restaurant has error");
                var routingKey = noticiation.GetType().Name.ToDashCase();

                _messageBus.Publish(noticiation, routingKey, "noticiation-service");

                throw new ValidationException("Validate Error");
            }

            await _restaurantRepository.Add(restaurant);

            foreach (var @event in restaurant.Events)
            {
                var routingKey = @event.GetType().Name.ToDashCase();

                _messageBus.Publish(@event, routingKey, "restaurant-service");
            }

            return restaurant.Id;
        }
    }
}
