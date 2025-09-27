// using NUnit.Framework;
// using SkillLink.API.Models;

// namespace SkillLink.Tests.Models
// {
//     [TestFixture]
//     public class SkillModelUnitTests
//     {
//         [Test]
//         public void Skill_DefaultValues_ShouldBeCorrect()
//         {
//             var skill = new Skill();

//             Assert.That(skill.SkillId, Is.EqualTo(0));
//             Assert.That(skill.Name, Is.EqualTo(string.Empty));
//             Assert.That(skill.IsPredefined, Is.False);
//         }

//         [Test]
//         public void UserSkill_DefaultValues_ShouldBeCorrect()
//         {
//             var userSkill = new UserSkill();

//             Assert.That(userSkill.UserSkillId, Is.EqualTo(0));
//             Assert.That(userSkill.UserId, Is.EqualTo(0));
//             Assert.That(userSkill.SkillId, Is.EqualTo(0));
//             Assert.That(userSkill.Level, Is.EqualTo("")); // default empty string
//             Assert.That(userSkill.Skill, Is.Null);
//         }

//         [Test]
//         public void AddSkillRequest_DefaultValues_ShouldBeCorrect()
//         {
//             var req = new AddSkillRequest();

//             Assert.That(req.UserId, Is.EqualTo(0));
//             Assert.That(req.SkillName, Is.EqualTo(string.Empty));
//             Assert.That(req.Level, Is.EqualTo("Beginner")); // custom default
//         }

//         [Test]
//         public void CanAssignValues_ToSkillAndUserSkill()
//         {
//             var skill = new Skill { SkillId = 1, Name = "C#", IsPredefined = true };
//             var userSkill = new UserSkill
//             {
//                 UserSkillId = 10,
//                 UserId = 5,
//                 SkillId = 1,
//                 Level = "Advanced",
//                 Skill = skill
//             };

//             Assert.That(userSkill.Skill, Is.Not.Null);
//             Assert.That(userSkill.Skill!.Name, Is.EqualTo("C#"));
//             Assert.That(userSkill.Level, Is.EqualTo("Advanced"));
//             Assert.That(userSkill.Skill.IsPredefined, Is.True);
//         }
//     }
// }
