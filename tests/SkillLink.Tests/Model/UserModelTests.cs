// using NUnit.Framework;
// using SkillLink.API.Models;

// namespace SkillLink.Tests.ModelTests
// {
//     [TestFixture]
//     public class UserModelTests
//     {
//         [Test]
//         public void User_Defaults_AreCorrect()
//         {
//             var u = new User();

//             // string defaults
//             Assert.That(u.FullName, Is.EqualTo(string.Empty));
//             Assert.That(u.Email, Is.EqualTo(string.Empty));
//             Assert.That(u.PasswordHash, Is.EqualTo(string.Empty));

//             // role & flags
//             Assert.That(u.Role, Is.EqualTo("Learner"));
//             Assert.That(u.EmailVerified, Is.False);
//             Assert.That(u.IsActive, Is.False);
//             Assert.That(u.ReadyToTeach, Is.False);

//             // nullable fields default null
//             Assert.That(u.Bio, Is.Null);
//             Assert.That(u.Location, Is.Null);
//             Assert.That(u.ProfilePicture, Is.Null);
//             Assert.That(u.EmailVerificationToken, Is.Null);
//             Assert.That(u.EmailVerificationExpires, Is.Null);

//             // CreatedAt is not explicitly initialized -> default(DateTime)
//             Assert.That(u.CreatedAt, Is.EqualTo(default(System.DateTime)));
//         }

//         [Test]
//         public void User_Properties_CanBeUpdated()
//         {
//             var u = new User
//             {
//                 UserId = 42,
//                 FullName = "Alice Lee",
//                 Email = "alice@uni.edu",
//                 PasswordHash = "hashed",
//                 Role = "Tutor",
//                 CreatedAt = System.DateTime.UtcNow,
//                 Bio = "Love teaching",
//                 Location = "Colombo",
//                 ProfilePicture = "/uploads/p/alice.png",
//                 ReadyToTeach = true,
//                 IsActive = true,
//                 EmailVerified = true,
//                 EmailVerificationToken = "tok123",
//                 EmailVerificationExpires = System.DateTime.UtcNow.AddHours(2)
//             };

//             Assert.That(u.UserId, Is.EqualTo(42));
//             Assert.That(u.FullName, Is.EqualTo("Alice Lee"));
//             Assert.That(u.Email, Is.EqualTo("alice@uni.edu"));
//             Assert.That(u.PasswordHash, Is.EqualTo("hashed"));
//             Assert.That(u.Role, Is.EqualTo("Tutor"));
//             Assert.That(u.Bio, Is.EqualTo("Love teaching"));
//             Assert.That(u.Location, Is.EqualTo("Colombo"));
//             Assert.That(u.ProfilePicture, Is.EqualTo("/uploads/p/alice.png"));
//             Assert.That(u.ReadyToTeach, Is.True);
//             Assert.That(u.IsActive, Is.True);
//             Assert.That(u.EmailVerified, Is.True);
//             Assert.That(u.EmailVerificationToken, Is.EqualTo("tok123"));
//             Assert.That(u.EmailVerificationExpires, Is.GreaterThan(System.DateTime.UtcNow));
//         }

//         [Test]
//         public void UpdateProfileRequest_Defaults_ThenSettable()
//         {
//             var r = new UpdateProfileRequest();
//             Assert.That(r.FullName, Is.EqualTo(string.Empty));
//             Assert.That(r.Bio, Is.Null);
//             Assert.That(r.Location, Is.Null);

//             r.FullName = "Bob";
//             r.Bio = "Bio here";
//             r.Location = "Kandy";

//             Assert.That(r.FullName, Is.EqualTo("Bob"));
//             Assert.That(r.Bio, Is.EqualTo("Bio here"));
//             Assert.That(r.Location, Is.EqualTo("Kandy"));
//         }

//         [Test]
//         public void LoginRequest_Defaults()
//         {
//             var lr = new LoginRequest();
//             Assert.That(lr.Email, Is.EqualTo(string.Empty));
//             Assert.That(lr.Password, Is.EqualTo(string.Empty));

//             lr.Email = "x@x.com";
//             lr.Password = "pass";
//             Assert.That(lr.Email, Is.EqualTo("x@x.com"));
//             Assert.That(lr.Password, Is.EqualTo("pass"));
//         }

//         [Test]
//         public void RegisterRequest_Defaults_AndSettable()
//         {
//             var rr = new RegisterRequest();
//             Assert.That(rr.FullName, Is.EqualTo(string.Empty));
//             Assert.That(rr.Email, Is.EqualTo(string.Empty));
//             Assert.That(rr.Password, Is.EqualTo(string.Empty));
//             Assert.That(rr.Role, Is.EqualTo("Learner"));
//             Assert.That(rr.ProfilePicture, Is.Null);
//             Assert.That(rr.ProfilePicturePath, Is.Null);

//             rr.FullName = "Carol";
//             rr.Email = "carol@uni.edu";
//             rr.Password = "secret";
//             rr.Role = "Tutor";
//             rr.ProfilePicturePath = "/uploads/p/carol.png";

//             Assert.That(rr.FullName, Is.EqualTo("Carol"));
//             Assert.That(rr.Email, Is.EqualTo("carol@uni.edu"));
//             Assert.That(rr.Password, Is.EqualTo("secret"));
//             Assert.That(rr.Role, Is.EqualTo("Tutor"));
//             Assert.That(rr.ProfilePicturePath, Is.EqualTo("/uploads/p/carol.png"));
//         }
//     }
// }
