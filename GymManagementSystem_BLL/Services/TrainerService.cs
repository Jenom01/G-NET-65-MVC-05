using GymManagementSystem_BLL.Helpers;
using GymManagementSystem_BLL.ViewModels.Trainers;
using GymManagementSystem_DAL.Models;
using GymManagementSystem_DAL.Models.ValueObjects;
using GymManagementSystem_DAL.Repositories;

namespace GymManagementSystem_BLL.Services
{
    public class TrainerService(ITrainerRepository trainers) : ITrainerService
    {
        public async Task<IEnumerable<TrainerViewModel>> GetAllAsync(CancellationToken ct = default)
        {
            var items = await trainers.GetAllAsync(ct);

            return items.Select(t => new TrainerViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Email = t.Email,
                Phone = t.Phone,
                Speciality = t.Speciality.GetDisplayName()
            });
        }

        public async Task<TrainerResult> CreateAsync(TrainerCreateViewModel model, CancellationToken ct = default)
        {
            var email = model.Email.Trim().ToLowerInvariant();
            var phone = model.Phone.Trim();

            if (await trainers.IsEmailTakenAsync(email, ct: ct))
                return TrainerResult.EmailExists;

            if (await trainers.IsPhoneTakenAsync(phone, ct: ct))
                return TrainerResult.PhoneExists;

            var trainer = new Trainer
            {
                Name = model.Name.Trim(),
                Email = email,
                Phone = phone,
                DateOfBirth = model.DateOfBirth!.Value,
                Gender = model.Gender!.Value,
                Speciality = model.Speciality!.Value,
                Address = new Address
                {
                    City = model.City.Trim(),
                    Street = model.Street.Trim(),
                    BuildingNumber = model.BuildingNumber!.Value
                }
            };

            trainers.Add(trainer);
            await trainers.SaveChangesAsync(ct);

            return TrainerResult.Success;
        }

        public async Task<TrainerDetailsViewModel?> GetDetailsAsync(int id, CancellationToken ct = default)
        {
            var trainer = await trainers.GetByIdAsync(id, ct);

            if (trainer is null) return null;

            return new TrainerDetailsViewModel
            {
                Id = id,
                Name = trainer.Name.Trim(),
                Email = trainer.Email,
                Phone = trainer.Phone,
                DateOfBirth = trainer.DateOfBirth,
                Address = $"{trainer.Address.BuildingNumber} - {trainer.Address.Street} - {trainer.Address.City}",
                Speciality = trainer.Speciality.GetDisplayName()
            };
        }

        public async Task<TrainerEditViewModel?> GetForUpdateAsync(int id, CancellationToken ct = default)
        {
            var trainer = await trainers.GetByIdAsync(id, ct);

            if (trainer is null) return null;

            return new TrainerEditViewModel
            {
                Name = trainer.Name,
                Email = trainer.Email,
                Phone = trainer.Phone,
                BuildingNumber = trainer.Address.BuildingNumber,
                Street = trainer.Address.Street,
                City = trainer.Address.City,
                Speciality = trainer.Speciality
            };
        }

        public async Task<TrainerResult> UpdateAsync(int id, TrainerEditViewModel model, CancellationToken ct = default)
        {
            var trainer = await trainers.GetByIdAsync(id, ct);

            if (trainer is null) return TrainerResult.NotFound;

            var email = model.Email.Trim().ToLowerInvariant();
            var phone = model.Phone.Trim();

            if (await trainers.IsEmailTakenAsync(email, trainer.Id, ct))
                return TrainerResult.EmailExists;
            if (await trainers.IsPhoneTakenAsync(phone, trainer.Id, ct))
                return TrainerResult.PhoneExists;

            trainer.Email = email;
            trainer.Phone = phone;
            trainer.Speciality = model.Speciality!.Value;
            trainer.Address.BuildingNumber = model.BuildingNumber!.Value;
            trainer.Address.Street = model.Street.Trim();
            trainer.Address.City = model.City.Trim();

            await trainers.SaveChangesAsync(ct);

            return TrainerResult.Success;
        }

        public async Task<TrainerResult> DeleteAsync(int id, CancellationToken ct = default)
        {
            var trainer = await trainers.GetByIdAsync(id, ct);

            if (trainer is null) return TrainerResult.NotFound;

            if (await trainers.HasActiveSessionsAsync(id, ct))
                return TrainerResult.HasSessions;

            trainers.Delete(trainer);
            await trainers.SaveChangesAsync(ct);

            return TrainerResult.Success;
        }
    }
}
