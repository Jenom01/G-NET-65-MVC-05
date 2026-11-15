using GymManagementSystem_BLL.ViewModels.Plans;
using GymManagementSystem_DAL.Repositories;

namespace GymManagementSystem_BLL.Services
{
    public class PlanService(IPlanRepository plans) : IPlanService
    {
        public async Task<IEnumerable<PlanViewModel>> GetAllAsync(CancellationToken ct = default)
        {
            var items = await plans.GetAllAsync(ct);

            return items.Select(p => new PlanViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                DurationDays = p.DurationDays,
                Price = p.Price,
                IsActive = p.IsActive
            });
        }

        public async Task<PlanViewModel?> GetDetailsAsync(int id, CancellationToken ct = default)
        {
            var plan = await plans.GetByIdAsync(id, ct);

            if (plan is null) return null;

            return new PlanViewModel
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                DurationDays = plan.DurationDays,
                Price = plan.Price,
                IsActive = plan.IsActive
            };
        }

        public async Task<PlanEditViewModel?> GetForUpdateAsync(int id, CancellationToken ct = default)
        {
            var plan = await plans.GetByIdAsync(id, ct);

            if (plan is null) return null;

            return new PlanEditViewModel
            {
                PlanName = plan.Name,
                DurationDays = plan.DurationDays,
                Price = plan.Price,
                Description = plan.Description
            };
        }

        public async Task<PlanResult> UpdateAsync(int id, PlanEditViewModel model, CancellationToken ct = default)
        {
            var plan = await plans.GetByIdAsync(id, ct);

            if (plan is null) return PlanResult.NotFound;

            if (await plans.HasActiveMembershipsAsync(id, ct))
                return PlanResult.HasActiveMemberships;

            plan.Description = model.Description.Trim();
            plan.DurationDays = model.DurationDays!.Value;
            plan.Price = model.Price!.Value;

            await plans.SaveChangesAsync(ct);

            return PlanResult.Success;
        }

        public async Task<PlanResult> ToggleStatusAsync(int id, CancellationToken ct = default)
        {
            var plan = await plans.GetByIdAsync(id, ct);

            if (plan is null) return PlanResult.NotFound;

            if (plan.IsActive && await plans.HasActiveMembershipsAsync(id, ct))
                return PlanResult.HasActiveMemberships;

            plan.IsActive = !plan.IsActive;
            await plans.SaveChangesAsync(ct);

            return PlanResult.Success;
        }
    }
}
