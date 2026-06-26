using AutoMapper;
using Shared.DataTransferObject;

namespace Service.Mapping
{
    public class PythonMealMappingProfile : Profile
    {
        public PythonMealMappingProfile()
        {
            CreateMap<PythonMealItemDto, MealIngredientDto>()
                .ForMember(dest => dest.FoodName, opt => opt.MapFrom(src => src.Food))
                .ForMember(dest => dest.Fats, opt => opt.MapFrom(src => src.Fat))
                .ForMember(dest => dest.Fibers, opt => opt.MapFrom(src => src.Fiber));

            CreateMap<KeyValuePair<string, List<PythonMealItemDto>>, MealDto>()
                .ForMember(dest => dest.MealType, opt => opt.MapFrom(src => src.Key))
                .ForMember(dest => dest.TotalCalories, opt => opt.MapFrom(src => src.Value.Sum(i => i.Calories)))
                .ForMember(dest => dest.Ingredients, opt => opt.MapFrom(src => src.Value));
        }
    }
}
