using CorrespondenceApi.Common;
using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace CorrespondenceApi.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedDomainAsync(db);
        await SeedAuthAsync(db);
    }

    private static async Task SeedDomainAsync(AppDbContext db)
    {
        if (await db.Organizations.IgnoreQueryFilters().AnyAsync()) return;

        // ---- Спільні довідники (не залежать від організації) ----
        var dIn = new DocumentDirection { Code = "IN", Name = "Вхідна" };
        var dOut = new DocumentDirection { Code = "OUT", Name = "Вихідна" };
        var dInt = new DocumentDirection { Code = "INT", Name = "Внутрішня" };

        var tLetter = new DocumentType { Name = "Лист", Code = "LST" };
        var tOrder = new DocumentType { Name = "Наказ", Code = "NAK" };
        var tRequest = new DocumentType { Name = "Запит", Code = "ZAP" };
        var tMemo = new DocumentType { Name = "Службова записка", Code = "SZ" };
        var tContract = new DocumentType { Name = "Договір", Code = "DOG" };
        var tAppeal = new DocumentType { Name = "Звернення", Code = "ZVN" };

        var sReg = new DocumentStatus { Name = "Зареєстровано" };
        var sReview = new DocumentStatus { Name = "На розгляді" };
        var sExec = new DocumentStatus { Name = "На виконанні" };
        var sDone = new DocumentStatus { Name = "Виконано", IsFinal = true };
        var sArch = new DocumentStatus { Name = "Архівовано", IsFinal = true };

        var chMail = new DeliveryChannel { Name = "Пошта" };
        var chEmail = new DeliveryChannel { Name = "Електронна пошта" };
        var chCourier = new DeliveryChannel { Name = "Кур'єр" };
        var chPersonal = new DeliveryChannel { Name = "Особисте подання" };

        var ctOrg = new CorrespondentType { Name = "Організація" };
        var ctPerson = new CorrespondentType { Name = "Фізична особа" };
        var ctGov = new CorrespondentType { Name = "Державна установа" };

        var tsAssigned = new TaskState { Name = "Призначено" };
        var tsInProgress = new TaskState { Name = "У роботі" };
        var tsDone = new TaskState { Name = "Виконано", IsFinal = true };
        var tsOverdue = new TaskState { Name = "Прострочено" };
        var tsCancelled = new TaskState { Name = "Скасовано", IsFinal = true };

        // ---- Організації (дерево) ----
        var org1 = new Organization { Name = "Підприємство «Промінь»", Code = "PROMIN",
            RegistrationNumberPattern = "{type}-{seq:000}/{year}" };
        var org1South = new Organization { Name = "Філія «Промінь-Південь»", Code = "PROMIN-S", Parent = org1 };
        var org2 = new Organization { Name = "ТОВ «Вектор»", Code = "VEKTOR",
            RegistrationNumberPattern = "{dir}-{seq:0000}/{yy}" };

        // ---- Підрозділи (дерево, у межах організації) ----
        var admin = new Department { Organization = org1, Name = "Адміністрація", ShortName = "Адмін." };
        var canc = new Department { Organization = org1, Name = "Канцелярія", ShortName = "Канц.", Parent = admin };
        var legal = new Department { Organization = org1, Name = "Юридичний відділ", ShortName = "Юрвідділ", Parent = admin };
        var hr = new Department { Organization = org1, Name = "Відділ кадрів", ShortName = "ВК", Parent = admin };
        var acc = new Department { Organization = org1, Name = "Бухгалтерія", ShortName = "Бух.", Parent = admin };

        var dir2 = new Department { Organization = org2, Name = "Дирекція", ShortName = "Дир." };
        var gen2 = new Department { Organization = org2, Name = "Загальний відділ", ShortName = "ЗВ", Parent = dir2 };

        // ---- Працівники ----
        var e1 = new Employee { Organization = org1, Department = canc, FullName = "Іваненко Іван Іванович", PositionName = "Реєстратор", Phone = "050-111-22-33", Email = "ivanenko@promin.ua" };
        var e2 = new Employee { Organization = org1, Department = admin, FullName = "Петренко Петро Петрович", PositionName = "Директор", Phone = "050-222-33-44", Email = "petrenko@promin.ua" };
        var e3 = new Employee { Organization = org1, Department = legal, FullName = "Сидоренко Олена Василівна", PositionName = "Юрисконсульт", Phone = "050-333-44-55", Email = "sydorenko@promin.ua" };
        var e4 = new Employee { Organization = org1, Department = hr, FullName = "Коваленко Марія Степанівна", PositionName = "Інспектор з кадрів", Email = "kovalenko@promin.ua" };
        var e5 = new Employee { Organization = org1, Department = acc, FullName = "Бондаренко Андрій Олегович", PositionName = "Головний бухгалтер" };
        var e6 = new Employee { Organization = org2, Department = dir2, FullName = "Орлов Сергій Миколайович", PositionName = "Директор", Email = "orlov@vektor.ua" };
        var e7 = new Employee { Organization = org2, Department = gen2, FullName = "Левченко Ірина Олександрівна", PositionName = "Діловод", Email = "levchenko@vektor.ua" };

        var cor1 = new Correspondent { Organization = org1, CorrespondentType = ctOrg, Name = "ТОВ «Промтехбуд»", Address = "м. Харків, вул. Сумська, 10", Phone = "057-700-10-20", ContactPerson = "Гнатюк О.П." };
        var cor2 = new Correspondent { Organization = org1, CorrespondentType = ctGov, Name = "Департамент освіти ХОДА", Address = "м. Харків, пл. Свободи, 5" };
        var cor3 = new Correspondent { Organization = org1, CorrespondentType = ctPerson, Name = "Шевченко Тарас Григорович", Address = "м. Харків, вул. Полтавський шлях, 44", Phone = "067-123-45-67" };
        var cor4 = new Correspondent { Organization = org1, CorrespondentType = ctOrg, Name = "АТ «Енергопостач»", Address = "м. Київ, вул. Хрещатик, 1", ContactPerson = "Мороз В.В." };
        var cor5 = new Correspondent { Organization = org2, CorrespondentType = ctOrg, Name = "ТОВ «Логістик-Плюс»", Address = "м. Дніпро, просп. Яворницького, 7" };

        DateTime At(string d) => DateTime.Parse(d + "T09:30:00");
        DateOnly D(string d) => DateOnly.Parse(d);

        // ---- Документи Org1 ----
        var doc1 = new Document { Organization = org1, Direction = dIn, DocumentType = tLetter, CurrentStatus = sExec, DeliveryChannel = chMail, Correspondent = cor1, ReceiverDepartment = legal, AuthorEmployee = e1, Title = "Лист щодо укладення договору постачання", Summary = "Пропозиція про співпрацю та укладення договору постачання матеріалів.", DocumentDate = D("2026-05-12"), IsControlled = true, Registration = new RegistrationRecord { RegistrationNumber = "101", RegistrationYear = 2026, RegisteredAt = At("2026-05-12"), RegistrarEmployee = e1 } };
        var res1 = new Resolution { Document = doc1, AuthorEmployee = e2, ResolutionText = "Підготувати проєкт договору", ResolutionDate = D("2026-05-13"), DueDate = D("2026-05-25"), IsControlled = true };
        doc1.Resolutions.Add(res1);
        doc1.Tasks.Add(new ExecutionTask { Resolution = res1, ExecutorEmployee = e3, TaskStatus = tsInProgress, TaskText = "Опрацювати умови та підготувати проєкт договору постачання", AssignedAt = At("2026-05-13"), DueDate = D("2026-05-25") });
        doc1.Tasks.Add(new ExecutionTask { Resolution = res1, ExecutorEmployee = e5, TaskStatus = tsDone, TaskText = "Погодити фінансові умови договору", AssignedAt = At("2026-05-13"), DueDate = D("2026-05-20"), CompletedAt = At("2026-05-19"), ResultText = "Умови погоджено" });

        var doc2 = new Document { Organization = org1, Direction = dIn, DocumentType = tRequest, CurrentStatus = sReview, DeliveryChannel = chEmail, Correspondent = cor2, ReceiverDepartment = hr, AuthorEmployee = e1, Title = "Запит про надання інформації", Summary = "Запит щодо кількості працівників із вищою освітою.", DocumentDate = D("2026-05-20"), IsControlled = true, Registration = new RegistrationRecord { RegistrationNumber = "102", RegistrationYear = 2026, RegisteredAt = At("2026-05-20"), RegistrarEmployee = e1 } };
        var res2 = new Resolution { Document = doc2, AuthorEmployee = e2, ResolutionText = "Надати відповідь", ResolutionDate = D("2026-05-21"), DueDate = D("2026-05-28"), IsControlled = true };
        doc2.Resolutions.Add(res2);
        doc2.Tasks.Add(new ExecutionTask { Resolution = res2, ExecutorEmployee = e4, TaskStatus = tsOverdue, TaskText = "Підготувати довідку про кількість працівників із вищою освітою", AssignedAt = At("2026-05-21"), DueDate = D("2026-05-28") });

        var doc3 = new Document { Organization = org1, Direction = dOut, DocumentType = tLetter, CurrentStatus = sDone, DeliveryChannel = chMail, Correspondent = cor4, SenderDepartment = acc, AuthorEmployee = e5, Title = "Відповідь на претензію щодо оплати", Summary = "Відповідь на претензію АТ «Енергопостач» щодо заборгованості.", DocumentDate = D("2026-05-22"), IsControlled = false, Registration = new RegistrationRecord { RegistrationNumber = "55", RegistrationYear = 2026, RegisteredAt = At("2026-05-22"), RegistrarEmployee = e1 } };

        var doc4 = new Document { Organization = org1, Direction = dInt, DocumentType = tMemo, CurrentStatus = sReg, DeliveryChannel = chPersonal, SenderDepartment = hr, ReceiverDepartment = admin, AuthorEmployee = e4, Title = "Службова записка про преміювання працівників", Summary = "Пропозиція щодо преміювання за результатами кварталу.", DocumentDate = D("2026-06-01"), IsControlled = false, Registration = new RegistrationRecord { RegistrationNumber = "12", RegistrationYear = 2026, RegisteredAt = At("2026-06-01"), RegistrarEmployee = e1 } };

        var doc5 = new Document { Organization = org1, Direction = dIn, DocumentType = tAppeal, CurrentStatus = sExec, DeliveryChannel = chPersonal, Correspondent = cor3, ReceiverDepartment = admin, AuthorEmployee = e1, Title = "Звернення громадянина щодо особистого прийому", Summary = "Звернення з проханням про особистий прийом керівником.", DocumentDate = D("2026-06-05"), IsControlled = true, Registration = new RegistrationRecord { RegistrationNumber = "103", RegistrationYear = 2026, RegisteredAt = At("2026-06-05"), RegistrarEmployee = e1 } };
        var res5 = new Resolution { Document = doc5, AuthorEmployee = e2, ResolutionText = "Розглянути звернення", ResolutionDate = D("2026-06-06"), DueDate = D("2026-06-20"), IsControlled = true };
        doc5.Resolutions.Add(res5);
        doc5.Tasks.Add(new ExecutionTask { Resolution = res5, ExecutorEmployee = e2, TaskStatus = tsAssigned, TaskText = "Розглянути звернення та призначити дату прийому", AssignedAt = At("2026-06-06"), DueDate = D("2026-06-20") });

        var doc6 = new Document { Organization = org1, Direction = dOut, DocumentType = tOrder, CurrentStatus = sArch, DeliveryChannel = chMail, Correspondent = cor1, SenderDepartment = admin, AuthorEmployee = e2, Title = "Наказ про затвердження плану заходів", DocumentDate = D("2026-04-30"), IsControlled = false, Registration = new RegistrationRecord { RegistrationNumber = "56", RegistrationYear = 2026, RegisteredAt = At("2026-04-30"), RegistrarEmployee = e1 } };

        // ---- Документ Org2 (для перевірки ізоляції) ----
        var doc7 = new Document { Organization = org2, Direction = dIn, DocumentType = tLetter, CurrentStatus = sReg, DeliveryChannel = chEmail, Correspondent = cor5, ReceiverDepartment = gen2, AuthorEmployee = e7, Title = "Лист про співпрацю з логістики", Summary = "Пропозиція логістичних послуг для ТОВ «Вектор».", DocumentDate = D("2026-06-10"), IsControlled = true, Registration = new RegistrationRecord { RegistrationNumber = "7", RegistrationYear = 2026, RegisteredAt = At("2026-06-10"), RegistrarEmployee = e7 } };

        db.AddRange(dIn, dOut, dInt, tLetter, tOrder, tRequest, tMemo, tContract, tAppeal,
            sReg, sReview, sExec, sDone, sArch, chMail, chEmail, chCourier, chPersonal,
            ctOrg, ctPerson, ctGov, tsAssigned, tsInProgress, tsDone, tsOverdue, tsCancelled,
            org1, org1South, org2, admin, canc, legal, hr, acc, dir2, gen2,
            e1, e2, e3, e4, e5, e6, e7, cor1, cor2, cor3, cor4, cor5,
            doc1, doc2, doc3, doc4, doc5, doc6, doc7);

        await db.SaveChangesAsync();

        // Реєстраційні номери: проставити організацію та згенерувати за патерном
        // (лічильник за організацією+типом+роком), щоб демодані відповідали автонумерації.
        var regSeq = new Dictionary<(int Org, int Type, int Year), int>();
        foreach (var d in new[] { doc1, doc2, doc3, doc4, doc5, doc6, doc7 })
        {
            var reg = d.Registration!;
            reg.OrganizationId = d.OrganizationId;
            var key = (d.OrganizationId, d.DocumentTypeId, reg.RegistrationYear);
            var n = regSeq.TryGetValue(key, out var v) ? v + 1 : 1;
            regSeq[key] = n;
            reg.SequenceNumber = n;
            reg.RegistrationNumber = RegistrationNumberFormatter.Format(
                d.Organization.RegistrationNumberPattern, n, reg.RegistrationYear,
                d.DocumentType.Code, d.Direction.Code);
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedAuthAsync(AppDbContext db)
    {
        if (await db.Permissions.AnyAsync()) return;

        var perms = Permissions.Catalog.ToDictionary(
            c => c.Code, c => new Permission { Code = c.Code, Category = c.Category, Description = c.Description });
        db.Permissions.AddRange(perms.Values);

        Role MakeRole(string name, string desc, params string[] codes)
        {
            var role = new Role { Name = name, Description = desc };
            role.RolePermissions = codes.Select(code => new RolePermission { Role = role, Permission = perms[code] }).ToList();
            return role;
        }

        // Супер-адмін — усі права (включно з organizations.manage)
        var superAdmin = MakeRole("Адміністратор", "Супер-адміністратор (усі організації)",
            Permissions.Catalog.Select(c => c.Code).ToArray());

        // Орг-адмін — усі права, КРІМ organizations.manage (обмежений своєю організацією)
        var orgAdmin = MakeRole("Адміністратор організації", "Адміністрування власної організації",
            Permissions.Catalog.Select(c => c.Code).Where(c => c != Permissions.OrganizationsManage).ToArray());

        var registrar = MakeRole("Реєстратор", "Канцелярія: реєстрація та облік",
            Permissions.DocumentsReadAll, Permissions.DocumentsCreate, Permissions.DocumentsUpdate,
            Permissions.DocumentsDelete, Permissions.CorrespondentsManage, Permissions.DictionariesManage,
            Permissions.EmployeesManage, Permissions.DepartmentsManage, Permissions.ReportsView,
            Permissions.DashboardOverview, Permissions.DashboardControl);

        var manager = MakeRole("Керівник", "Розгляд документів і резолюції",
            Permissions.DocumentsReadInvolved, Permissions.DocumentsReadDepartment,
            Permissions.ResolutionsCreate, Permissions.TasksAssign, Permissions.ReportsView,
            Permissions.DashboardOverview, Permissions.DashboardControl);

        var executor = MakeRole("Виконавець", "Виконання доручень",
            Permissions.DocumentsReadInvolved, Permissions.TasksExecute, Permissions.DashboardMyTasks);

        db.Roles.AddRange(superAdmin, orgAdmin, registrar, manager, executor);

        var org1 = await db.Organizations.IgnoreQueryFilters().FirstAsync(o => o.Code == "PROMIN");
        var org2 = await db.Organizations.IgnoreQueryFilters().FirstAsync(o => o.Code == "VEKTOR");
        async Task<Employee?> Emp(string surname) =>
            await db.Employees.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.FullName.StartsWith(surname));

        AppUser MakeUser(string userName, string password, string fullName, Organization? org, Employee? emp, Role role)
        {
            var u = new AppUser
            {
                UserName = userName,
                PasswordHash = PasswordHasher.Hash(password),
                FullName = fullName,
                Organization = org,
                Employee = emp,
            };
            u.UserRoles = [new UserRole { User = u, Role = role }];
            return u;
        }

        db.Users.AddRange(
            MakeUser("admin", "admin123", "Супер-адміністратор", null, null, superAdmin),
            MakeUser("registrar", "registrar123", "Іваненко Іван Іванович", org1, await Emp("Іваненко"), registrar),
            MakeUser("manager", "manager123", "Петренко Петро Петрович", org1, await Emp("Петренко"), manager),
            MakeUser("executor", "executor123", "Сидоренко Олена Василівна", org1, await Emp("Сидоренко"), executor),
            MakeUser("orgadmin1", "orgadmin123", "Адмін «Промінь»", org1, await Emp("Коваленко"), orgAdmin),
            MakeUser("orgadmin2", "orgadmin123", "Адмін «Вектор»", org2, await Emp("Орлов"), orgAdmin));

        await db.SaveChangesAsync();
    }
}
