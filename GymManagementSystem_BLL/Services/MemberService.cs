using GymManagementSystem_BLL.Helpers;
using GymManagementSystem_BLL.ViewModels.HealthRecords;
using GymManagementSystem_BLL.ViewModels.Members;
using GymManagementSystem_DAL.Models;
using GymManagementSystem_DAL.Models.ValueObjects;
using GymManagementSystem_DAL.Repositories;

namespace GymManagementSystem_BLL.Services
{
    public class MemberService(IUnitOfWork uow) : IMemberService
    {
        public async Task<IEnumerable<MemberIndexViewModel>> GetAllAsync(CancellationToken ct = default)
        {
            var items = await uow.Members.GetAllAsync(ct);

            return items.Select(m => new MemberIndexViewModel
            {
                Id = m.Id,
                Name = m.Name,
                Email = m.Email,
                Phone = m.Phone,
                PhotoUrl = m.Photo,
                JoinDate = m.JoinDate ?? m.CreatedAt,
                Gender = m.Gender.ToString()
            });
        }

        public async Task<MemberResult> CreateAsync(MemberCreateViewModel model, CancellationToken ct)
        {
            var email = model.Email.Trim().ToLowerInvariant();
            var phone = model.Phone.Trim();

            if (await uow.Members.IsEmailTakenAsync(email, ct: ct))
                return MemberResult.EmailExists;

            if (await uow.Members.IsPhoneTakenAsync(phone, ct: ct))
                return MemberResult.PhoneExists;

            var member = new Member
            {
                Name = model.Name.Trim(),
                Email = email,
                Phone = phone,
                DateOfBirth = model.DateOfBirth!.Value,
                Gender = model.Gender!.Value,
                Address = new Address
                {
                    City = model.City.Trim(),
                    Street = model.Street.Trim(),
                    BuildingNumber = model.BuildingNumber!.Value
                },
                HealthRecord = new HealthRecord
                {
                    Height = model.HealthRecord.Height!.Value,
                    Weight = model.HealthRecord.Weight!.Value,
                    BloodType = model.HealthRecord.BloodType!.Value,
                    Note = model.HealthRecord.Note?.Trim()
                }
            };

            uow.Members.Add(member);
            await uow.SaveChangesAsync(ct);

            return MemberResult.Success;
        }

        public async Task<MemberDetailsViewModel?> GetDetailsAsync(int id, CancellationToken ct = default)
        {
            var member = await uow.Members.GetDetailsAsync(id, ct);

            if (member is null) return null;

            var now = DateTime.UtcNow;

            var activeMembership = member.Memberships
                .Where(ms => ms.StartDate <= now && ms.EndDate > now)
                .OrderByDescending(ms => ms.EndDate)
                .FirstOrDefault();

            return new MemberDetailsViewModel
            {
                Id = id,
                Name = member.Name.Trim(),
                PhotoUrl = member.Photo,
                Email = member.Email,
                Phone = member.Phone,
                Gender = member.Gender.ToString(),
                DateOfBirth = member.DateOfBirth.ToShortDateString(),
                Address = $"{member.Address.BuildingNumber} - {member.Address.Street} - {member.Address.City}",

                PlanName = activeMembership?.Plan.Name ?? "No Active Plan",
                MembershipStartDate = activeMembership?.StartDate.ToShortDateString() ?? "N/A",
                MembershipEndDate = activeMembership?.EndDate.ToShortDateString() ?? "N/A"
            };
        }

        public async Task<HealthRecordDetailsViewModel?> GetHealthRecordAsync(int memberId, CancellationToken ct = default)
        {
            var healthRecord = await uow.HealthRecords.GetWhereAsync(hr => hr.MemberId == memberId, ct);

            if (healthRecord is null) return null;

            return new HealthRecordDetailsViewModel
            {
                Height = healthRecord.Height,
                Weight = healthRecord.Weight,
                BloodType = healthRecord.BloodType.GetDisplayName(),
                Note = healthRecord.Note ?? "No Notes"
            };
        }

        public async Task<MemberEditViewModel?> GetForUpdateAsync(int id, CancellationToken ct = default)
        {
            var member = await uow.Members.GetByIdAsync(id, ct);

            if (member is null) return null;

            return new MemberEditViewModel
            {
                Name = member.Name,
                PhotoUrl = member.Photo,
                Email = member.Email,
                Phone = member.Phone,
                BuildingNumber = member.Address.BuildingNumber,
                City = member.Address.City,
                Street = member.Address.Street
            };
        }

        public async Task<MemberResult> UpdateAsync(int id, MemberEditViewModel model, CancellationToken ct = default)
        {
            var member = await uow.Members.GetByIdAsync(id, ct);

            if (member is null) return MemberResult.NotFound;

            var email = model.Email.Trim().ToLowerInvariant();
            var phone = model.Phone.Trim();

            if (await uow.Members.IsEmailTakenAsync(email, member.Id, ct))
                return MemberResult.EmailExists;

            if (await uow.Members.IsPhoneTakenAsync(phone, member.Id, ct))
                return MemberResult.PhoneExists;

            member.Email = email;
            member.Phone = phone;
            member.Address.City = model.City.Trim();
            member.Address.Street = model.Street.Trim();
            member.Address.BuildingNumber = model.BuildingNumber!.Value;

            await uow.SaveChangesAsync(ct);

            return MemberResult.Success;
        }

        public async Task<MemberResult> DeleteAsync(int id, CancellationToken ct = default)
        {
            var member = await uow.Members.GetByIdAsync(id, ct, m => m.HealthRecord);

            if (member is null) return MemberResult.NotFound;

            if (await uow.Members.HasActiveBookingsAsync(id, ct))
                return MemberResult.HasActiveBookings;

            if (member.HealthRecord is not null)
                uow.HealthRecords.Delete(member.HealthRecord);

            uow.Members.Delete(member);
            await uow.SaveChangesAsync(ct);

            return MemberResult.Success;
        }
    }
}
