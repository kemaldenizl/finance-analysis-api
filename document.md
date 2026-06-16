# Finance Analysis API Proje Raporu

## 1. Giriş

### 1.1 Projenin Genel Tanımı

Bu rapor, `finance-analysis-api` projesinin teknik yapısını, kullandığı materyalleri, uyguladığı metotları, ortaya koyduğu yazılım özelliklerini ve elde edilen sistem çıktısını dört ana başlık altında değerlendirir. Proje; finansal analiz uygulaması için güvenli kimlik doğrulama, oturum yönetimi, API yönlendirme, finansal işlem verisi alma, dosya işleme entegrasyonu ve AI destekli analiz akışlarını destekleyen .NET tabanlı bir backend çözümüdür.

Projenin merkezinde iki ana servis ve bir API Gateway bulunur. `Security.API`, kullanıcı ve güvenlik yönetimini üstlenir. `Transactions.WebAPI`, finansal işlem dosyalarının harici bir Inputs API servisine aktarılması, çıkarım/normalizasyon işlemlerinin tetiklenmesi ve AI analiz akışlarının yönetilmesi için adapter görevi görür. `FinanceAnalysis.ApiGateway` ise istemciden gelen istekleri güvenlik ve yönlendirme katmanından geçirerek ilgili servislere ulaştırır.

### 1.2 Projenin Amacı

Projenin amacı, finansal analiz sisteminin backend tarafında güvenilir, test edilebilir, genişletilebilir ve güvenlik odaklı bir altyapı kurmaktır. Bu amaç doğrultusunda proje; kullanıcı kayıt, giriş, token yenileme, çıkış, çok faktörlü doğrulama, e-posta doğrulama, şifre sıfırlama, yetki yönetimi, denetim kayıtları, rate limiting, health check, container tabanlı çalışma ortamı ve finansal işlem/AI servis entegrasyonlarını bir araya getirir.

| Amaç | Projedeki Karşılığı |
|---|---|
| Güvenli kullanıcı yönetimi | Register, login, logout, refresh token, password reset, e-posta doğrulama |
| Oturum güvenliği | Refresh session, token rotation, token reuse detection, Redis tabanlı token invalidation |
| Çok faktörlü doğrulama | TOTP MFA, MFA challenge token, recovery code üretimi ve doğrulaması |
| Yetkilendirme | Role, permission ve permission policy yapısı |
| Finansal veri işleme entegrasyonu | Dosya yükleme, extraction, normalization, AI save ve AI chat endpointleri |
| Merkezi yönlendirme | YARP tabanlı API Gateway |
| İzlenebilirlik | Audit log, correlation id, Serilog, health check |
| Çalıştırılabilir ortam | Docker Compose ile PostgreSQL, Redis, pgAdmin, Security API, Transactions API ve Gateway |
| Test edilebilirlik | Testcontainers destekli entegrasyon testleri |

Tablo, projenin yalnızca bir authentication servisi olmadığını; finansal analiz uygulamasının güvenlik, servis iletişimi, veri işleme ve operasyonel çalışma ihtiyaçlarını birlikte ele aldığını gösterir.

### 1.3 Projenin Kapsamı

Projenin kapsamı üç ana fonksiyonel bölüme ayrılır:

| Kapsam | Açıklama | İlgili Proje |
|---|---|---|
| Güvenlik ve kimlik yönetimi | Kullanıcı oluşturma, giriş, çıkış, token yenileme, MFA, şifre sıfırlama, e-posta doğrulama | `Security.API`, `Security.Application`, `Security.Domain`, `Security.Infrastructure` |
| API yönlendirme | İstemci isteklerinin ilgili backend servisine yönlendirilmesi, CORS, JWT doğrulama ve gateway rate limit | `FinanceAnalysis.ApiGateway` |
| Finansal işlem entegrasyonu | Dosya yükleme, dosya türüne göre extraction, normalization, AI analiz kaydı ve AI chat | `Transactions.WebAPI` |

Bu kapsamda Security servisi projenin en geniş ve olgun bölümüdür. Transactions servisi daha çok başka bir analiz/inputs servisine geçiş yapan ince bir entegrasyon katmanıdır. Gateway ise istemci ile servisler arasında tek giriş noktası oluşturur.

### 1.4 Problem Tanımı

Finansal analiz uygulamalarında kullanıcıların hesap, işlem ve belge verileri hassastır. Bu nedenle backend tarafında şu ihtiyaçlar ortaya çıkar:

- Kullanıcı hesaplarının güçlü şifre politikalarıyla korunması.
- Access token ve refresh token kullanımının güvenli yönetilmesi.
- Token çalınması veya tekrar kullanımı gibi risklerin azaltılması.
- Kullanıcı oturumlarının cihaz/session bazında izlenebilmesi.
- Kritik işlemlerin audit log ile kayıt altına alınması.
- Hassas işlemlerde rate limiting uygulanması.
- Şifre sıfırlama ve e-posta doğrulama gibi akışların güvenli tasarlanması.
- Hesap güvenliğini artırmak için MFA desteği sağlanması.
- Finansal işlem dosyalarının analiz servisine kontrollü biçimde iletilmesi.
- Servislerin container ortamında birlikte çalıştırılabilmesi.

Bu proje, yukarıdaki ihtiyaçlara katmanlı bir .NET backend mimarisiyle cevap verir.

### 1.5 Projenin Genel Mimari Özeti

Sistem mimarisi servis odaklıdır. Gateway dış giriş noktasıdır. Security API kimlik ve güvenlik işlemlerini yürütür. Transactions API, finansal işlem işleme süreçlerini harici Inputs API ile haberleşerek yönetir. PostgreSQL kalıcı veri saklama, Redis ise token invalidation ve cache altyapısı için kullanılır.

| Bileşen | Rolü | Ana Teknoloji |
|---|---|---|
| `FinanceAnalysis.ApiGateway` | İstek yönlendirme, CORS, JWT doğrulama, gateway rate limit | ASP.NET Core, YARP |
| `Security.API` | HTTP endpointleri, middleware, problem details, health check | ASP.NET Core Minimal API |
| `Security.Application` | Use-case handler'ları, validasyon, result modeli | MediatR, FluentValidation |
| `Security.Domain` | Domain entity ve iş kuralları | Saf C# domain modeli |
| `Security.Infrastructure` | Veritabanı, Redis, JWT, e-posta, MFA, repository | EF Core, PostgreSQL, Redis, Resend, Otp.NET |
| `Transactions.WebAPI` | Finansal dosya ve AI endpoint adapter'ı | ASP.NET Core Controller, HttpClient |
| `tests` | Güvenlik servisinin doğrulanması | xUnit, Testcontainers |

Bu mimariyle iş kuralları, HTTP yüzeyi ve altyapı bağımlılıkları birbirinden ayrılmıştır. Özellikle Security servisinde Clean Architecture yaklaşımı belirgindir.

## 2. Materyal ve Metod

### 2.1 Kullanılan Teknolojiler

Projede kullanılan temel materyaller yazılım kütüphaneleri, çalışma ortamları ve servis bağımlılıklarıdır.

| Kategori | Teknoloji | Projedeki Kullanım Amacı |
|---|---|---|
| Backend platformu | .NET 9 | Tüm API servislerinin geliştirilmesi |
| HTTP API | ASP.NET Core Minimal API | Security endpointlerinin tanımlanması |
| HTTP API | ASP.NET Core Controller | Transactions endpointlerinin tanımlanması |
| Reverse proxy | YARP | Gateway üzerinden servis yönlendirme |
| Veritabanı | PostgreSQL 16 | Kullanıcı, rol, token, session, MFA ve audit verilerinin saklanması |
| ORM | Entity Framework Core 9 | Veritabanı erişimi, migration ve model mapping |
| Cache/Revocation | Redis 7 | Access token revoke, user/session invalidation kayıtları |
| CQRS/mediator | MediatR | Command/query handler mimarisi |
| Validasyon | FluentValidation | Request ve command doğrulama |
| Authentication | JWT Bearer | Access token doğrulama |
| Şifreleme/koruma | ASP.NET Data Protection | TOTP secret ve MFA challenge token koruması |
| MFA | Otp.NET | TOTP secret üretme ve TOTP kod doğrulama |
| E-posta | Resend | Verification ve password reset e-postası gönderme |
| Loglama | Serilog | Yapılandırılmış log üretimi |
| Health check | ASP.NET Core HealthChecks | API, PostgreSQL ve Redis sağlık kontrolü |
| Test | xUnit, FluentAssertions | Test senaryoları |
| Test altyapısı | Testcontainers | PostgreSQL ve Redis entegrasyon testleri |
| Container | Docker Compose | Servisleri birlikte çalıştırma |

Bu teknoloji seçimi, projenin hem geliştirme hem de entegrasyon testleri için gerçekçi bir servis ortamında çalışmasını sağlar.

### 2.2 Proje Yapısı ve Katmanlandırma Metodu

Security servisi dört ayrı katmandan oluşur. Bu katmanlandırma, sorumlulukların ayrılmasını ve sistemin sürdürülebilirliğini sağlar.

| Katman | Görev | İçerik |
|---|---|---|
| API | HTTP istek/cevap sınırı | Endpointler, middleware, ProblemDetails, OpenAPI, health check |
| Application | İş akışı | Command/query handler, validator, DTO, result modeli, abstraction arayüzleri |
| Domain | İş modeli | User, Role, Permission, RefreshSession, Token, MFA, Audit entity'leri |
| Infrastructure | Dış bağımlılıklar | EF Core, repository, PostgreSQL, Redis, JWT, e-posta, token generator |

Bu metotta API katmanı doğrudan iş mantığı yazmaz. Endpointler request modellerini command/query nesnelerine çevirir ve MediatR aracılığıyla Application katmanına iletir. Application katmanı Domain modellerini kullanır ancak veritabanı, Redis, e-posta veya JWT gibi altyapıları arayüzler üzerinden tanır. Infrastructure katmanı bu arayüzlerin gerçek implementasyonlarını sağlar.

### 2.3 Çözüm ve Dosya Organizasyonu

Projenin ana dizinleri ve işlevleri şu şekildedir:

| Yol | Açıklama |
|---|---|
| `finance-analysis-api.sln` | Çözüm dosyası |
| `docker-compose.yml` | Tüm servislerin container ortamı |
| `readme.md` | Projenin kapsamlı teknik açıklaması |
| `src/ApiGateways/FinanceAnalysis.ApiGateway` | Reverse proxy ve gateway servisi |
| `src/Services/Security/Security.API` | Security HTTP API |
| `src/Services/Security/Security.Application` | Security iş akışları ve validasyon |
| `src/Services/Security/Security.Domain` | Security domain modeli |
| `src/Services/Security/Security.Infrastructure` | Security altyapı implementasyonları |
| `src/Services/Transactions/Transactions.WebAPI` | Finansal işlem ve AI adapter API |
| `tests/Security.IntegrationTests` | Security entegrasyon testleri |
| `tests/Security.ArchitectureTests` | Mimari test projesi |
| `tests/Security.UnitTests` | Unit test projesi |

Bu yapı, projenin farklı sorumluluklarını ayrı projeler halinde yönetmeyi sağlar.

### 2.4 API Gateway Metodu

Gateway, istemci ile backend servisleri arasında tek giriş noktasıdır. ASP.NET Core üzerine kurulmuş ve YARP Reverse Proxy kullanılarak yapılandırılmıştır.

Gateway'de uygulanan yöntemler:

- İzinli frontend origin'leri için CORS policy tanımlama.
- JWT Bearer token doğrulama.
- `authenticated` adlı authorization policy tanımlama.
- Dakikada 100 istek izin veren sabit pencere rate limiter kullanma.
- `/health` endpoint'i ile gateway durumunu döndürme.
- Route/cluster yapılandırmasını `ReverseProxy` config bölümünden okuma.

| Gateway Route | Hedef Servis | Açıklama |
|---|---|---|
| `/api/auth/{**catch-all}` | Security API | Register, login, refresh, logout, password reset, e-posta doğrulama |
| `/api/mfa/{**catch-all}` | Security API | TOTP MFA kurulum, login doğrulama, disable, recovery code |
| `/api/sessions/{**catch-all}` | Security API | Kullanıcı session listeleme ve revoke |
| `/api/users/{**catch-all}` | Security API | Mevcut kullanıcı bilgisi |
| `/api/test/{**catch-all}` | Security API | Auth ve permission test endpointleri |
| `/api/transactions/{**catch-all}` | Transactions API | Finansal işlem ve AI entegrasyon endpointleri |

Transactions route'u gateway seviyesinde authenticated policy ile korunur. Security route'larında asıl yetkilendirme endpoint seviyesinde uygulanır.

### 2.5 Security.API Metodu

Security.API, Minimal API yaklaşımıyla geliştirilmiştir. Başlatma sırasında şu metodoloji izlenir:

| Adım | Yapılan İşlem |
|---|---|
| 1 | Serilog yapılandırılır |
| 2 | Application ve Infrastructure bağımlılıkları eklenir |
| 3 | Rate limiting policy'leri tanımlanır |
| 4 | ProblemDetails ve exception handler eklenir |
| 5 | OpenAPI eklenir |
| 6 | Health check tanımları yapılır |
| 7 | Correlation id ve log enrichment middleware eklenir |
| 8 | Development/IntegrationTests ortamlarında migration ve seed çalıştırılır |
| 9 | Authentication ve Authorization aktif edilir |
| 10 | Auth, MFA, Session, User ve Test endpointleri map edilir |

Security.API, HTTP yüzeyini sağlarken iş mantığını MediatR handler'larına devreder. Bu sayede endpointler ince, test edilebilir ve okunabilir kalır.

### 2.6 Application Katmanı Metodu

Application katmanı, CQRS benzeri bir yapı kullanır. Her iş akışı bir command veya query ile temsil edilir.

| İş Akışı | Command/Query | Handler'ın Temel Görevi |
|---|---|---|
| Kullanıcı kaydı | `RegisterCommand` | Kullanıcı oluşturma, şifre hashleme, verification token üretme |
| Giriş | `LoginCommand` | Kimlik doğrulama, session oluşturma, access/refresh token üretme veya MFA challenge |
| Token yenileme | `RefreshTokenCommand` | Refresh token rotation ve reuse detection |
| Çıkış | `LogoutCommand` | Mevcut session ve token invalidation |
| Tüm cihazlardan çıkış | `LogoutAllCommand` | Tüm session'ları revoke etme |
| Şifre sıfırlama başlatma | `ForgotPasswordCommand` | Reset token üretme ve e-posta gönderme |
| Şifre sıfırlama tamamlama | `ResetPasswordCommand` | Şifre değiştirme, tüm session'ları revoke etme |
| E-posta doğrulama | `VerifyEmailCommand` | Verification token doğrulama |
| MFA kurulum başlatma | `BeginMfaSetupCommand` | TOTP secret üretme ve koruma |
| MFA kurulum tamamlama | `CompleteMfaSetupCommand` | TOTP doğrulama, recovery code üretme |
| MFA login tamamlama | `CompleteMfaLoginCommand` | Challenge token ve TOTP/recovery code doğrulama |
| MFA kapatma | `DisableMfaCommand` | TOTP veya recovery code ile MFA disable |
| Recovery code yenileme | `RegenerateRecoveryCodesCommand` | Yeni recovery code üretme |
| Session listeleme | `GetMySessionsQuery` | Kullanıcı session'larını listeleme |
| Session revoke | `RevokeSessionCommand` | Seçili session'ı kapatma |

Bu metot, her use-case'in tek bir handler içinde anlaşılır biçimde yönetilmesini sağlar.

### 2.7 Validasyon Metodu

FluentValidation, Application katmanında MediatR pipeline davranışı olarak kullanılır. `ValidationBehavior`, handler çalışmadan önce request doğrulamasını yapar.

| Alan | Kural |
|---|---|
| E-posta | Boş olamaz, maksimum 320 karakter, e-posta formatında olmalı |
| Register/Reset şifresi | Minimum 12, maksimum 200 karakter, büyük harf, küçük harf, rakam ve özel karakter içermeli |
| Login şifresi | Boş olamaz, maksimum 200 karakter |
| Refresh/verification/reset token | Boş olamaz, maksimum 2048 karakter |
| Access token jti | Boş olamaz, maksimum 200 karakter |
| TOTP kodu | 6-8 karakter, sadece rakam |
| Recovery code | Minimum 8, maksimum 64 karakter |
| MFA doğrulama | TOTP veya recovery code alanlarından en az biri verilmeli |

Validasyon hataları `validation.invalid` kodlu result failure'a çevrilir ve API katmanında `400 Bad Request` validation problem response olarak döner.

### 2.8 Domain Modelleme Metodu

Domain katmanında kullanıcı, rol, yetki, session, token, MFA ve audit kavramları ayrı entity/aggregate yapılarıyla modellenmiştir.

| Domain Nesnesi | Görevi |
|---|---|
| `User` | Kullanıcı hesabı, e-posta, şifre hash'i, aktiflik, e-posta doğrulama ve rol ilişkisi |
| `UserRole` | Kullanıcı-rol ilişkisi |
| `Role` | Rol adı ve permission ilişkileri |
| `Permission` | Yetki kodu |
| `RolePermission` | Rol-permission ilişkisi |
| `RefreshSession` | Kullanıcının cihaz/session bazlı refresh token grubu |
| `RefreshToken` | Refresh token hash'i, expiry, consumed ve revoked durumu |
| `EmailVerificationToken` | E-posta doğrulama token hash'i ve kullanım durumu |
| `PasswordResetToken` | Şifre sıfırlama token hash'i ve kullanım durumu |
| `MfaMethod` | Kullanıcıya ait TOTP MFA kaydı |
| `RecoveryCode` | MFA recovery code hash'i ve kullanım durumu |
| `AuditLog` | Kritik olayların denetim kaydı |

Domain modellerinde constructor ve method seviyesinde guard kontrolleri kullanılır. Boş Guid, boş string, default tarih veya geçersiz durumlar engellenir.

### 2.9 Persistence ve Veritabanı Metodu

Security verileri PostgreSQL üzerinde `security` schema'sında tutulur. EF Core DbContext ve entity configuration sınıflarıyla tablo, index ve ilişki yapıları tanımlanır.

| Tablo | İçerik |
|---|---|
| `users` | Kullanıcı temel bilgileri |
| `user_roles` | Kullanıcı-rol ilişkileri |
| `roles` | Rol bilgileri |
| `permissions` | Permission kodları |
| `role_permissions` | Rol-permission ilişkileri |
| `refresh_sessions` | Session kayıtları |
| `refresh_tokens` | Refresh token hash ve durum kayıtları |
| `email_verification_tokens` | E-posta doğrulama tokenları |
| `password_reset_tokens` | Şifre sıfırlama tokenları |
| `mfa_methods` | TOTP MFA method kayıtları |
| `recovery_codes` | MFA recovery code kayıtları |
| `audit_logs` | Güvenlik olay logları |

Önemli index ve constraint yaklaşımları:

| Alan | Amaç |
|---|---|
| `users.normalized_email` unique | Aynı e-posta ile tekrar kullanıcı açılmasını engelleme |
| `roles.normalized_name` unique | Rol adlarını benzersiz tutma |
| `permissions.code` unique | Permission kodlarını benzersiz tutma |
| `refresh_tokens.token_hash` unique | Refresh token tekrarlarını engelleme |
| `email_verification_tokens.token_hash` unique | Verification token benzersizliği |
| `password_reset_tokens.token_hash` unique | Reset token benzersizliği |
| `mfa_methods.user_id` unique | Kullanıcı başına tek MFA kaydı |
| `audit_logs.user_id`, `audit_logs.created_at_utc` | Audit sorgularını hızlandırma |

### 2.10 Redis ile Token Invalidation Metodu

JWT access token'lar stateless olduğu için logout veya password reset gibi durumlarda token'ın geçersiz kılınması Redis ile sağlanır.

| Invalidation Türü | Ne Zaman Kullanılır | Etki |
|---|---|---|
| JTI bazlı revocation | Logout sırasında mevcut access token için | Tek token geçersiz olur |
| Session bazlı invalidation | Logout veya session revoke sırasında | Session'a ait tokenlar geçersiz olur |
| User bazlı invalidation | Logout-all veya password reset sırasında | Kullanıcının tüm tokenları geçersiz olur |

JWT doğrulama sırasında Redis kayıtları kontrol edilir. Token'ın jti değeri, kullanıcı invalidation zamanı ve session invalidation zamanı access token'ın geçerliliğini etkiler.

### 2.11 MFA Metodu

MFA mekanizması TOTP tabanlıdır. Secret üretimi Otp.NET ile yapılır, secret değeri Data Protection ile korunarak veritabanında encrypted olarak saklanır. Ayrıca hash değeri de tutulur.

| MFA Adımı | Açıklama |
|---|---|
| Setup begin | Secret üretilir, korunur, otpauth URI oluşturulur |
| Setup complete | Kullanıcı TOTP kodu gönderir, doğrulanır, MFA aktif edilir |
| Recovery code üretimi | 10 adet tek kullanımlık recovery code üretilir |
| MFA login challenge | Login başarılı ancak MFA aktifse access token yerine challenge token döner |
| MFA login complete | TOTP veya recovery code ile challenge tamamlanır |
| MFA disable | TOTP veya recovery code ile MFA kapatılır |
| Recovery code regenerate | TOTP ile yeni recovery code seti oluşturulur |

Recovery code'lar plaintext olarak yalnızca üretildikleri anda response'ta verilir. Veritabanında sadece hash saklanır.

### 2.12 Transactions.WebAPI Metodu

Transactions servisi, kendi başına finansal analiz algoritması çalıştırmaz. Harici `InputsApi` servisine istek gönderen bir adapter katmanıdır.

| Endpoint | Metot | Harici Çağrı | Amaç |
|---|---|---|---|
| `/api/transactions/file-input` | POST multipart | `/v1/inputs` | Kullanıcı dosyasını Inputs API'ye iletmek |
| `/api/transactions/file-extract` | POST JSON | `/v1/extractions/{type}/{input_id}` ve `/v1/normalizations/{input_id}` | Dosya extraction ve normalization sürecini tetiklemek |
| `/api/transactions/ai-save` | POST JSON | `/v1/ai/analyze-and-save` | AI analiz sonucunu kaydetmek |
| `/api/transactions/ai-chat` | POST JSON | `/v1/ai/chat` | Analizle ilgili AI soru-cevap akışı sağlamak |

Dosya yükleme endpoint'inde 50 MB request size limiti vardır. Dosya metadata bilgileri response içine eklenir. `file-extract` endpoint'i `file_name` değerine göre extraction tipini `image` veya `pdf` olarak belirler.

### 2.13 Docker ve Çalışma Ortamı Metodu

Docker Compose ile sistemin bütün bileşenleri birlikte ayağa kaldırılır.

| Servis | Container | Görev |
|---|---|---|
| `security-redis` | `security-redis` | Redis cache ve token revocation altyapısı |
| `kemal-db` | `kemal_db` | PostgreSQL veritabanı |
| `pgadmin` | `pgadmin` | Veritabanı yönetim arayüzü |
| `security-api` | `security-api` | Güvenlik API'si |
| `transactions-api` | `transactions-api` | Finansal işlem adapter API'si |
| `api-gateway` | `finance-api-gateway` | Reverse proxy ve dış giriş noktası |

Port eşlemeleri:

| Servis | Port |
|---|---|
| Gateway | `8080:8080` |
| Security API | `7001:8080` |
| Transactions API | `7002:8080` |
| Redis | `${REDIS_PORT}:6379` |
| PostgreSQL | `${POSTGRES_PORT}:5432` |
| pgAdmin | `${PGADMIN_PORT}:80` |

Bu yapı, geliştirme ortamında servislerin tek komutla çalıştırılmasını sağlar.

### 2.14 Test Metodu

Proje test stratejisinde özellikle Security servisi öne çıkar. Entegrasyon testleri gerçek HTTP pipeline'a yakın şekilde çalışır ve PostgreSQL/Redis için Testcontainers kullanır.

| Test Grubu | Test Dosyaları | Doğrulanan Davranış |
|---|---|---|
| Auth | Register, login, refresh, logout, e-posta doğrulama, password reset | Kimlik doğrulama akışları |
| Refresh güvenliği | Refresh reuse detection | Token tekrar kullanımında session revoke |
| Revoked token | Revoked access token testleri | Redis tabanlı token invalidation |
| Session | SessionManagementTests | Session listeleme ve revoke |
| Hardening | LogoutAll, MFA, PasswordResetSession, SingleSessionRevoke | Güvenlik güçlendirme senaryoları |
| Health | HealthChecks, CorrelationId | Sağlık kontrolü ve izlenebilirlik |
| Rate limiting | RateLimitingTests | Hassas endpointlerde istek sınırlama |

Unit test projesi ve architecture test projesi de çözümde yer alır. En kapsamlı doğrulama ise integration test tarafındadır.

## 3. Bulgular

### 3.1 Sistem Bileşenlerine Ait Bulgular

Proje incelendiğinde, sistemin tek parça bir API yerine servisler ve katmanlar halinde tasarlandığı görülür. Security servisi kimlik ve güvenlik işlevlerini merkezi olarak toplar. Transactions servisi finansal veri işleme sürecinde harici Inputs API ile bağlantı kurar. Gateway servisleri istemciye tek bir API yüzeyi gibi sunar.

| Bileşen | Bulgular |
|---|---|
| Gateway | YARP ile route bazlı yönlendirme, JWT doğrulama, CORS ve rate limiting içerir |
| Security API | En kapsamlı servis olup authentication, authorization, MFA, session ve audit özelliklerini sunar |
| Transactions API | Dosya/AI işlemleri için harici Inputs API'ye geçiş katmanı olarak tasarlanmıştır |
| PostgreSQL | Security domain verilerinin kalıcı saklanması için kullanılır |
| Redis | Access token revocation ve invalidation için kullanılır |
| Docker Compose | Geliştirme ortamında bütün bağımlılıkları birlikte ayağa kaldırır |

Bu bulgular, projenin güvenlik servisleri ve finansal analiz entegrasyonu için ayrıştırılmış bir backend altyapısı oluşturduğunu gösterir.

### 3.2 Security Servisinin Fonksiyonel Bulguları

Security servisi çok sayıda kullanıcı güvenliği işlevini birlikte sunar.

| Fonksiyon | Durum | Açıklama |
|---|---|---|
| Kullanıcı kaydı | Var | E-posta ve güçlü şifre validasyonu ile kullanıcı oluşturulur |
| Login | Var | E-posta/şifre doğrulanır, session ve token üretilir |
| Refresh token | Var | Refresh token rotation yapılır |
| Refresh reuse detection | Var | Kullanılmış/revoked token tekrar kullanılırsa session kapatılır |
| Logout | Var | Mevcut session kapatılır ve access token revoke edilir |
| Logout-all | Var | Kullanıcının tüm session'ları kapatılır |
| Password reset | Var | Token ile şifre değiştirilir, tüm session'lar revoke edilir |
| E-posta doğrulama | Var | 24 saatlik verification token ile doğrulama yapılır |
| MFA/TOTP | Var | Kurulum, doğrulama, login challenge, disable ve recovery code desteklenir |
| Session yönetimi | Var | Session listeleme ve tek session revoke desteklenir |
| Permission policy | Var | Role-permission claim'leri üzerinden authorization yapılır |
| Audit log | Var | Kritik güvenlik olayları kayıt altına alınır |
| Health check | Var | Liveness ve readiness endpointleri vardır |
| Rate limiting | Var | Hassas endpointlerde sabit pencere limit uygulanır |

Bu tablo Security servisinin yalnızca temel login/register yapmadığını, güvenlik yaşam döngüsünü uçtan uca yönettiğini gösterir.

### 3.3 Auth Akışlarına Ait Bulgular

Auth endpointleri `/api/auth` altında toplanmıştır.

| Endpoint | Başarılı Response | Temel İşlev |
|---|---:|---|
| `POST /api/auth/register` | `201 Created` | Kullanıcı oluşturur, e-posta doğrulama token'ı üretir |
| `POST /api/auth/login` | `200 OK` | Kullanıcıyı doğrular, token döner veya MFA challenge başlatır |
| `POST /api/auth/refresh` | `200 OK` | Refresh token'ı consume eder, yeni token çifti üretir |
| `POST /api/auth/logout` | `204 No Content` | Mevcut session'ı revoke eder |
| `POST /api/auth/logout-all` | `204 No Content` | Tüm session'ları revoke eder |
| `POST /api/auth/forgot-password` | `202 Accepted` | Password reset token üretir |
| `POST /api/auth/reset-password` | `204 No Content` | Şifreyi değiştirir, session'ları kapatır |
| `POST /api/auth/verify-email` | `204 No Content` | E-posta doğrulama token'ını kullanır |
| `POST /api/auth/resend-verification` | `202 Accepted` | Yeni doğrulama token'ı üretir |

Auth akışlarında güvenlik açısından önemli bulgu, tokenların plaintext olarak veritabanında tutulmamasıdır. Refresh token, password reset token ve email verification token hash olarak saklanır.

### 3.4 MFA Bulguları

MFA yapısı TOTP standardını kullanır ve kullanıcı hesabına ikinci doğrulama katmanı ekler.

| MFA Özelliği | Açıklama |
|---|---|
| TOTP secret üretimi | Otp.NET ile random secret üretilir |
| Secret koruması | Data Protection ile encrypted olarak saklanır |
| Secret hash'i | SHA-256 hash ayrıca tutulur |
| OTP Auth URI | Authenticator uygulamalarına ekleme için URI oluşturulur |
| Recovery code | 10 adet tek kullanımlık recovery code üretilir |
| Login challenge | MFA aktifse login access token yerine challenge token döner |
| Recovery code kullanımı | TOTP yoksa recovery code ile doğrulama yapılabilir |
| Disable MFA | TOTP veya recovery code ile MFA kapatılabilir |
| Code regeneration | TOTP doğrulamasıyla recovery code'lar yenilenebilir |

Bu bulgular, MFA'nın yalnızca basit bir kod kontrolü değil; setup, doğrulama, recovery, disable ve yeniden üretim yaşam döngüsüne sahip olduğunu gösterir.

### 3.5 Session ve Token Güvenliği Bulguları

Projede session ve token güvenliğine özel önem verilmiştir.

| Mekanizma | Bulgular |
|---|---|
| Refresh session | Her login işleminde device/user agent ve IP bilgisiyle session oluşturulur |
| Refresh token rotation | Her refresh çağrısında eski token consumed olur, yeni token üretilir |
| Reuse detection | Consumed/revoked refresh token yeniden kullanılırsa session kapatılır |
| Token hashleme | Refresh tokenlar veritabanında hash olarak saklanır |
| Logout invalidation | Mevcut access token jti Redis'te revoke edilir |
| Session invalidation | Session bazlı Redis invalidation ile access token geçersizleşir |
| User invalidation | Logout-all ve password reset sonrası kullanıcı bazlı invalidation yapılır |
| Password reset hardening | Şifre değişince tüm session'lar ve tokenlar geçersiz olur |

Bu yapı, JWT'nin stateless doğasından kaynaklanan logout zorluğunu Redis ile çözer.

### 3.6 Yetkilendirme Bulguları

Yetkilendirme role-permission modeliyle kurgulanmıştır.

| Permission Kodu | Anlamı |
|---|---|
| `users.read` | Kullanıcı okuma yetkisi |
| `users.manage` | Kullanıcı yönetme yetkisi |
| `roles.read` | Rol okuma yetkisi |
| `roles.manage` | Rol yönetme yetkisi |
| `permissions.read` | Permission okuma yetkisi |
| `permissions.manage` | Permission yönetme yetkisi |
| `sessions.read` | Session okuma yetkisi |
| `sessions.manage` | Session yönetme yetkisi |

Seed mekanizması bu permission'ları oluşturur, `Admin` rolünü ekler ve tüm permission'ları Admin rolüne atar. Admin kullanıcı development ortamında seed ile oluşturulur.

### 3.7 Audit ve İzlenebilirlik Bulguları

Sistemde kritik güvenlik olayları audit log olarak tutulur.

| Audit Olayı | Örnek Durum |
|---|---|
| `UserRegistered` | Yeni kullanıcı oluşturulduğunda |
| `LoginSucceeded` / `LoginFailed` | Başarılı veya başarısız login |
| `RefreshSucceeded` / `RefreshFailed` | Refresh token kullanımı |
| `RefreshReuseDetected` | Token tekrar kullanımı tespit edildiğinde |
| `LogoutCurrentSession` | Mevcut session kapatıldığında |
| `LogoutAllSessions` | Tüm session'lar kapatıldığında |
| `SessionRevoked` | Tekil session revoke edildiğinde |
| `EmailVerificationRequested` / `EmailVerified` | E-posta doğrulama süreci |
| `PasswordResetRequested` / `PasswordResetCompleted` | Şifre sıfırlama süreci |
| `MfaSetupStarted` / `MfaEnabled` | MFA kurulumu |
| `MfaLoginChallenged` / `MfaLoginCompleted` | MFA login süreci |
| `MfaRecoveryCodeUsed` | Recovery code kullanımı |
| `MfaDisabled` | MFA kapatma |
| `RecoveryCodesRegenerated` | Recovery code yenileme |

Audit kayıtlarında user id, IP address, user agent, correlation id, payload JSON ve oluşturulma zamanı tutulur. Bu, güvenlik olaylarının sonradan incelenebilmesini sağlar.

### 3.8 Rate Limiting Bulguları

Security API hassas endpointler için ayrı rate limit policy'leri tanımlar.

| Policy | Limit | Pencere |
|---|---:|---:|
| Register | 3 | 60 saniye |
| Login | 5 | 60 saniye |
| Refresh | 10 | 60 saniye |
| Logout | 10 | 60 saniye |
| Sessions | 20 | 60 saniye |
| ForgotPassword | 3 | 60 saniye |
| ResetPassword | 5 | 60 saniye |
| VerifyEmail | 10 | 60 saniye |
| ResendVerification | 3 | 60 saniye |

Register, login ve password reset gibi brute-force veya abuse riski yüksek işlemlerde limitlerin düşük tutulduğu görülür. Limit aşıldığında `429 Too Many Requests` ProblemDetails response döner.

### 3.9 Health Check ve Operasyonel Bulgular

Security API iki health endpoint sunar.

| Endpoint | Tip | Kontrol |
|---|---|---|
| `/health/live` | Liveness | API process ayakta mı |
| `/health/ready` | Readiness | PostgreSQL ve Redis hazır mı |

Gateway tarafında ayrıca `/health` endpoint'i bulunur ve `Gateway OK` durumunu döndürür. Bu yapı container orchestration veya manuel izleme süreçlerinde servislerin durumunu anlamayı kolaylaştırır.

### 3.10 Transactions Servisine Ait Bulgular

Transactions servisi finansal işlem verilerini işleyen ana algoritmayı içermez; harici Inputs API ile haberleşen geçiş katmanıdır.

| Özellik | Bulgular |
|---|---|
| Dosya yükleme | Kullanıcı id ve dosyayı multipart form ile Inputs API'ye gönderir |
| Dosya boyutu | `file-input` için 50 MB limit uygulanır |
| Dosya türü eşleme | `scanned_pdf`, `camera_photo`, `screenshot` image; `real_pdf` pdf olarak işlenir |
| Extraction | `/v1/extractions/{type}/{input_id}` çağrısı yapılır |
| Normalization | Extraction sonrası `/v1/normalizations/{input_id}` çağrısı yapılır |
| AI save | AI analiz ve kayıt isteği harici servise aktarılır |
| AI chat | Kullanıcı sorusu analiz bağlamında harici AI chat endpoint'ine gönderilir |
| Response parsing | Harici response JSON ise JSON, değilse string olarak sarılır |

Bu bulgular, projenin finansal analiz tarafında belge/işlem verisini dış analiz altyapısına bağlayan bir backend arayüzü sunduğunu gösterir.

### 3.11 Veritabanı ve Migration Bulguları

Security veritabanı EF Core migration'larıyla yönetilir. Mevcut migration'lar:

| Migration | Açıklama |
|---|---|
| `20260331134159_InitialSecuritySchema` | İlk security schema ve temel tablolar |
| `20260602124242_AddMfaTotpSupport` | TOTP MFA ve recovery code desteği |

Development ve IntegrationTests ortamlarında uygulama başlangıcında migration çalışır ve identity seed yapılır. Staging ortamında migration çalışır. Production için otomatik migration yolu açık değildir.

### 3.12 Test Bulguları

Integration test kapsamı, Security servisinin kritik davranışlarını doğrulamaya odaklanır.

| Test Alanı | Bulgular |
|---|---|
| Register/login/refresh/logout | Temel authentication yaşam döngüsü test edilir |
| E-posta doğrulama | Verification token akışı test edilir |
| Password reset | Reset token ve session invalidation test edilir |
| Refresh reuse detection | Tekrar kullanılan refresh token'ın session'ı kapattığı doğrulanır |
| Revoked access token | Redis invalidation sonrası access token kullanımı engellenir |
| MFA hardening | MFA setup, login, recovery code ve disable akışları doğrulanır |
| Logout-all hardening | Tüm session ve token invalidation doğrulanır |
| Single session revoke | Tek session kapatmanın etkisi test edilir |
| Health check | Liveness/readiness response'ları test edilir |
| Correlation id | Correlation id header davranışı test edilir |
| Rate limiting | Hassas endpointlerde limit aşımı test edilir |

Testcontainers kullanımı, testlerin yalnızca mock seviyesinde kalmadığını, gerçek PostgreSQL ve Redis container'larıyla entegrasyon düzeyinde çalışabildiğini gösterir.

### 3.13 Güçlü Yönlere Ait Bulgular

| Güçlü Yön | Açıklama |
|---|---|
| Katmanlı mimari | API, Application, Domain ve Infrastructure net ayrılmıştır |
| Güvenlik kapsamı | Token rotation, MFA, password reset hardening ve audit log birlikte uygulanmıştır |
| Redis invalidation | JWT logout ve session invalidation problemi iyi ele alınmıştır |
| Test altyapısı | Kritik güvenlik akışları integration testlerle desteklenmiştir |
| Container desteği | Geliştirme ortamı Docker Compose ile kolay ayağa kalkar |
| Extensibility | Application arayüzleri sayesinde altyapı değiştirilebilir |
| Observability | Correlation id, Serilog ve health check mevcuttur |

Bu güçlü yönler, projenin bitirme projesi kapsamında yalnızca çalışan endpointlerden oluşmadığını; güvenlik ve mimari kaliteyi de hedeflediğini gösterir.

### 3.14 Sınırlılıklar ve Dikkat Edilecek Noktalar

| Nokta | Değerlendirme |
|---|---|
| Inputs API repo içinde değil | Transactions servisinin gerçek analiz davranışı harici servise bağlıdır |
| Transactions servisinde kendi JWT doğrulaması yok | Koruma gateway seviyesinde yapılır |
| Gateway Redis revocation kontrolü yapmaz | Revocation kontrolünün güçlü tarafı Security.API içindedir |
| Resend API key zorunlu | Lokal/container ortamında eksik ayar uygulama başlangıcını etkileyebilir |
| Unit test projesi iskelet halinde | En güçlü test kapsamı integration test tarafındadır |
| Production migration otomasyonu sınırlı | Kodda otomatik migration Development/IntegrationTests/Staging için düşünülmüştür |

Bu noktalar projenin mevcut davranışını anlamak için önemlidir. Sistemin ana işlevleri tamamlanmış olsa da bazı bileşenler harici bağımlılıklara veya deployment kararlarına bağlıdır.

## 4. Sonuç

### 4.1 Genel Değerlendirme

Finance Analysis API projesi, finansal analiz uygulaması için güvenlik merkezli bir backend altyapısı ortaya koymaktadır. Proje; kullanıcı hesap yönetimi, token güvenliği, çok faktörlü doğrulama, session yönetimi, role-permission yetkilendirme, audit logging, health check, rate limiting, container tabanlı çalışma ve finansal işlem/AI servis entegrasyonu gibi birçok özelliği tek çözüm içinde toplamıştır.

Security servisi, projenin en detaylı ve olgun kısmıdır. Register/login gibi temel işlemlerin yanında refresh token rotation, refresh token reuse detection, Redis destekli token invalidation, password reset sonrası tüm session'ların kapatılması ve MFA gibi ileri güvenlik mekanizmaları uygulanmıştır. Bu özellikler, kullanıcı verilerinin hassas olduğu finansal uygulama bağlamında önemli bir güvenlik seviyesi sağlar.

Transactions servisi, finansal işlem belgelerinin ve AI analiz akışlarının harici Inputs API ile entegre edilmesini sağlar. Bu servis, dosya yükleme, extraction, normalization, AI analiz kaydı ve AI chat gibi fonksiyonları tek HTTP arayüzünde toplar. Gateway ise bu servisleri istemci açısından tek bir backend API gibi sunar.

### 4.2 Projenin Sağladığı Kazanımlar

| Kazanım | Açıklama |
|---|---|
| Güvenli kimlik doğrulama | JWT, refresh token, güçlü şifre politikası ve MFA ile güvenli giriş altyapısı |
| Yönetilebilir oturumlar | Kullanıcı session listeleme, tek session revoke, logout-all ve password reset invalidation |
| Token güvenliği | Hash saklama, rotation, reuse detection ve Redis revocation |
| Yetki kontrolü | Role-permission modeli ve policy tabanlı authorization |
| İzlenebilirlik | Audit log, correlation id ve structured logging |
| Operasyonel hazır olma | Health checks, Docker Compose ve servis bağımlılıklarının tanımlı olması |
| Finansal analiz entegrasyonu | Dosya, extraction, normalization ve AI endpointlerinin harici analiz servisine bağlanması |
| Test güveni | Testcontainers ile gerçek bağımlılıklara yakın entegrasyon testleri |

Bu kazanımlar, projenin hem fonksiyonel hem de güvenlik açısından güçlü bir backend temel sunduğunu gösterir.

### 4.3 Dört Ana Başlık Açısından Nihai Yorum

| Başlık | Nihai Değerlendirme |
|---|---|
| Giriş | Proje, finansal analiz uygulamasının güvenli backend altyapısını kurmayı hedefler |
| Materyal ve Metod | .NET 9, Clean Architecture, MediatR, EF Core, PostgreSQL, Redis, JWT, MFA, Docker ve Testcontainers kullanılmıştır |
| Bulgular | Security servisi güçlü ve kapsamlıdır; Transactions servisi harici analiz API'sine bağlantı sağlar; Gateway merkezi yönlendirme sunar |
| Sonuç | Proje, finansal analiz sistemi için güvenli, modüler, test edilebilir ve container ortamında çalıştırılabilir bir backend çözümü üretmiştir |

### 4.4 Son Söz

Sonuç olarak bu proje, finansal analiz uygulaması için yalnızca veri alan bir API değil; güvenlik, kimlik, session, token, MFA, audit ve servis yönlendirme ihtiyaçlarını birlikte ele alan bütüncül bir backend mimarisidir. Proje özellikle Security servisi tarafında güçlü bir mühendislik yaklaşımı sergiler. Transactions servisi ve Gateway ile birlikte değerlendirildiğinde sistem; kullanıcı güvenliğini sağlayan, finansal işlem verisini harici analiz servislerine yönlendiren ve container tabanlı ortamda çalışabilen kapsamlı bir backend çözümü haline gelmiştir.
