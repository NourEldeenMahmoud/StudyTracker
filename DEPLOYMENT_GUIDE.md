# دليل نشر المشروع على ASP Monster

## الخطوات المطلوبة:

### 1. إعداد قاعدة البيانات
1. أنشئ قاعدة بيانات SQL Server في ASP Monster Control Panel
2. احصل على Connection String من ASP Monster (عادة يكون في Control Panel > Databases)
3. استبدل `YOUR_PRODUCTION_CONNECTION_STRING_HERE` في `appsettings.Production.json`

**مثال Connection String:**
```
Server=YOUR_SERVER_NAME;Database=YOUR_DB_NAME;User Id=YOUR_USERNAME;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true
```

### 2. إعداد مجلد الصور
- الصور تُحفظ في `wwwroot/images/profiles` (يتم إنشاؤها تلقائياً)
- تأكد من أن مجلد `wwwroot` موجود وله صلاحيات الكتابة

### 3. نشر المشروع

#### الطريقة الأولى: باستخدام Visual Studio
1. Right-click على المشروع > Publish
2. اختر "Folder" أو "FTP"
3. اختر Configuration: Release
4. اضغط Publish
5. ارفع محتويات مجلد `publish` إلى السيرفر

#### الطريقة الثانية: باستخدام dotnet CLI
```bash
# في مجلد المشروع
dotnet publish -c Release -o ./publish

# ثم ارفع محتويات مجلد publish إلى السيرفر
```

### 4. إعداد Connection String في ASP Monster
- في ASP Monster Control Panel:
  - اذهب إلى Application Settings أو Environment Variables
  - أضف Connection String باسم `ConnectionStrings:DefaultConnection`
  - أو عدل `appsettings.Production.json` مباشرة على السيرفر

### 5. تشغيل Migrations
بعد الرفع، شغل migrations لإنشاء الجداول:

```bash
# عبر SSH أو Terminal في ASP Monster
cd /path/to/your/app
dotnet ef database update
```

**أو** استخدم Package Manager Console في Visual Studio:
```powershell
Update-Database
```

### 6. إعداد Environment Variables
في ASP Monster Control Panel:
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://localhost:5000` (أو حسب إعدادات ASP Monster)

### 7. إعداد Admin User
- بعد تشغيل Migrations، سيتم إنشاء Admin user تلقائياً
- تحقق من `DataSeeder.cs` لمعرفة بيانات Admin الافتراضية
- **مهم:** غير كلمة المرور بعد أول تسجيل دخول!

### 8. اختبار الموقع
افتح الموقع وتحقق من:
- ✅ تسجيل الدخول (Register/Login)
- ✅ رفع الصور (Profile Picture)
- ✅ إضافة Sessions
- ✅ Leaderboard
- ✅ Admin Panel (إذا كنت admin)

## الملفات المهمة للنشر:

1. **appsettings.Production.json** - إعدادات Production
2. **wwwroot/** - الملفات الثابتة (CSS, JS, Images)
3. **bin/Release/net8.0/publish/** - ملفات النشر

## ملاحظات مهمة:

1. **Connection String Format:**
   ```
   Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true
   ```

2. **File Upload:**
   - الصور تُحفظ في `wwwroot/images/profiles`
   - المجلد يتم إنشاؤه تلقائياً عند أول رفع صورة
   - تأكد من صلاحيات الكتابة على مجلد `wwwroot`

3. **HTTPS:**
   - ASP Monster عادة يدعم HTTPS
   - تأكد من إعداد SSL certificate في Control Panel

4. **Database:**
   - تأكد من أن SQL Server متاح في ASP Monster
   - بعض الخطط قد تحتاج ترقية لدعم SQL Server

5. **.NET Version:**
   - المشروع يستخدم .NET 8.0
   - تأكد من أن ASP Monster يدعم .NET 8.0

## استكشاف الأخطاء:

### مشكلة: Database connection failed
- تحقق من Connection String
- تأكد من أن SQL Server يعمل
- تحقق من Firewall settings

### مشكلة: Images not loading
- تحقق من صلاحيات مجلد `wwwroot/images/profiles`
- تأكد من أن Static Files middleware يعمل

### مشكلة: 500 Internal Server Error
- تحقق من Logs في ASP Monster
- تأكد من أن جميع NuGet packages موجودة
- تحقق من Environment Variables
