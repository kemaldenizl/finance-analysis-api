# Finance Analysis API Proje Dokümantasyonu

Bu doküman, `finance-analysis-api` deposunun mevcut kod yapısını, servislerini, mimarisini, güvenlik akışlarını, veri modelini, konfigürasyonlarını, Docker ortamını ve test kapsamını açıklar. Kod tabanı incelenerek hazırlanmıştır; proje dosyalarına veya kaynak koda herhangi bir değişiklik yapılmamıştır.

## 1. Projenin Amacı ve Genel Yapısı

Bu proje .NET 9 ile geliştirilmiş, mikroservis yaklaşımına yakın bir backend çözümüdür. Çözümün merkezinde kullanıcı kimlik doğrulama, oturum yönetimi, JWT üretimi, refresh token rotasyonu, e-posta doğrulama, şifre sıfırlama, MFA/TOTP ve yetkilendirme işlevlerini sunan `Security` servisi yer alır.

Projede ayrıca:

- `FinanceAnalysis.ApiGateway`: Dış dünyadan gelen istekleri ilgili servislere yönlendiren YARP tabanlı API Gateway.
- `Security.API`: Kimlik doğrulama ve güvenlik uçlarını sunan ana servis.
- `Security.Application`: Use-case/iş akışı katmanı. MediatR command/query handler'ları, validasyonlar ve uygulama soyutlamaları burada bulunur.
- `Security.Domain`: Domain entity'leri, aggregate'ler, iş kuralları ve temel domain tipleri.
- `Security.Infrastructure`: PostgreSQL, Redis, JWT, e-posta, token üretimi, MFA, repository ve EF Core altyapısı.
- `Transactions.WebAPI`: Finansal işlem dosyası yükleme, çıkarım, normalizasyon ve AI analiz uçlarına proxy/adapter görevi gören servis.
- `tests`: Security servisine yönelik entegrasyon, mimari ve unit test projeleri.

Çözüm dosyası:

```text
finance-analysis-api.sln
```

Ana çalışma ortamı Docker Compose ile PostgreSQL, Redis, pgAdmin, Security API, Transactions API ve API Gateway servislerini birlikte ayağa kaldıracak şekilde hazırlanmıştır.

## 2. Teknoloji Yığını

Proje genelinde kullanılan ana teknolojiler:

- .NET 9
- ASP.NET Core Minimal API ve Controller tabanlı API
- Entity Framework Core 9
- PostgreSQL 16
- Redis 7
- YARP Reverse Proxy
- MediatR
- FluentValidation
- JWT Bearer Authentication
- ASP.NET Core Authorization Policies
- ASP.NET Core Rate Limiting
- ASP.NET Core Health Checks
- Serilog
- Data Protection
- Otp.NET ile TOTP MFA
- Resend ile e-posta gönderimi
- Testcontainers ile entegrasyon test altyapısı
- xUnit ve FluentAssertions
- Docker ve Docker Compose

## 3. Dizin Yapısı

Proje kökündeki önemli dosya ve klasörler:

```text
.
├── docker-compose.yml
├── finance-analysis-api.sln
├── project_documentation.md
├── src
│   ├── ApiGateways
│   │   └── FinanceAnalysis.ApiGateway
│   └── Services
│       ├── Security
│       │   ├── Security.API
│       │   ├── Security.Application
│       │   ├── Security.Domain
│       │   └── Security.Infrastructure
│       └── Transactions
│           └── Transactions.WebAPI
└── tests
    ├── Security.ArchitectureTests
    ├── Security.IntegrationTests
    └── Security.UnitTests
```

### 3.1 API Gateway

```text
src/ApiGateways/FinanceAnalysis.ApiGateway
```

Gateway projesi YARP kullanır. Gelen HTTP isteklerini route tanımlarına göre `security-api` ve `transactions-api` servislerine yönlendirir. Ayrıca CORS, JWT Bearer Authentication, basit rate limiting ve health endpoint'i içerir.

### 3.2 Security Servisi

```text
src/Services/Security
```

Security servisi dört katmana ayrılmıştır:

- `Security.API`: HTTP endpointleri, middleware, problem details, health check ve request/response contract'ları.
- `Security.Application`: Command/query handler'ları, DTO'lar, validasyonlar, result modeli ve port/abstraction arayüzleri.
- `Security.Domain`: User, Role, Permission, RefreshSession, Token, MFA ve Audit domain modelleri.
- `Security.Infrastructure`: EF Core DbContext, repository implementasyonları, PostgreSQL/Redis bağlantısı, JWT, password hashing, token üreticileri, e-posta ve seed mekanizması.

Bu yapı Clean Architecture / katmanlı mimariye yakındır. Domain katmanı altyapıyı bilmez; Application katmanı arayüzlere bağımlıdır; Infrastructure bu arayüzleri gerçek teknolojilerle uygular; API katmanı HTTP yüzeyini sunar.

### 3.3 Transactions Servisi

```text
src/Services/Transactions/Transactions.WebAPI
```

Transactions servisi ASP.NET Core Controller kullanır. Kendi veritabanı yoktur. `InputsApi` adlı dış/ayrı bir API'ye HTTP istekleri gönderir. Dosya yükleme, extraction, normalization, AI analiz kaydetme ve AI chat işlemleri için adapter görevi görür.

### 3.4 Test Projeleri

```text
tests/Security.IntegrationTests
tests/Security.ArchitectureTests
tests/Security.UnitTests
```

En dolu test projesi `Security.IntegrationTests` projesidir. PostgreSQL ve Redis için Testcontainers kullanır. Auth, MFA, session, rate limiting, health check, correlation id ve hardening senaryolarını kapsayan test dosyaları vardır.

## 4. Servisler ve Çalışma Portları

`docker-compose.yml` içinde tanımlı servisler:

| Servis | Amaç | Container | Port |
|---|---|---|---|
| `security-redis` | Access token revocation ve cache altyapısı | `security-redis` | `${REDIS_PORT}:6379` |
| `kemal-db` | PostgreSQL veritabanı | `kemal_db` | `${POSTGRES_PORT}:5432` |
| `pgadmin` | PostgreSQL yönetim arayüzü | `pgadmin` | `${PGADMIN_PORT}:80` |
| `security-api` | Kimlik ve güvenlik API'si | `security-api` | `7001:8080` |
| `transactions-api` | Transaction/AI adapter API'si | `transactions-api` | `7002:8080` |
| `api-gateway` | Dış API Gateway | `finance-api-gateway` | `8080:8080` |

Docker Compose ortamında gateway dış dünyaya `8080` portundan açılır. Security API ve Transactions API gateway arkasında çalışır; doğrudan erişim için ayrıca `7001` ve `7002` portları da publish edilmiştir.

## 5. API Gateway Detayları

Gateway giriş noktası:

```text
src/ApiGateways/FinanceAnalysis.ApiGateway/Program.cs
```

Gateway'in sorumlulukları:

- Frontend kaynaklarına CORS izni vermek.
- JWT Bearer Authentication yapılandırmak.
- `authenticated` authorization policy'si tanımlamak.
- Sabit pencere rate limiter eklemek.
- YARP reverse proxy yapılandırmasını `ReverseProxy` config bölümünden okumak.
- `/health` endpoint'i ile basit gateway sağlık durumu döndürmek.

### 5.1 Gateway CORS

`Cors:AllowedOrigins` config bölümünden izinli origin listesi okunur. Varsayılan değerler:

```text
http://localhost:3000
https://localhost:3000
```

### 5.2 Gateway JWT Ayarları

Gateway JWT doğrulamasında:

- issuer doğrulanır,
- audience doğrulanır,
- signing key doğrulanır,
- token lifetime doğrulanır,
- clock skew sıfırdır.

Development config içinde `Jwt:Key` ve `Jwt:SigningKey` aynı dummy geliştirme anahtarını içerir.

### 5.3 Gateway Route'ları

`appsettings.Development.json` içinde tanımlı route'lar:

| Route | Path | Hedef Cluster | Authorization |
|---|---|---|---|
| `security-auth-route` | `/api/auth/{**catch-all}` | `security-cluster` | Gateway seviyesinde zorunlu değil |
| `security-mfa-route` | `/api/mfa/{**catch-all}` | `security-cluster` | Endpoint bazlı |
| `security-sessions-route` | `/api/sessions/{**catch-all}` | `security-cluster` | Endpoint bazlı |
| `security-users-route` | `/api/users/{**catch-all}` | `security-cluster` | Endpoint bazlı |
| `security-test-route` | `/api/test/{**catch-all}` | `security-cluster` | Endpoint bazlı |
| `transactions-base-route` | `/api/transactions/{**catch-all}` | `transactions-cluster` | `authenticated` |

Transactions route'u gateway seviyesinde authenticated policy ister. Security route'larında yetkilendirme asıl Security.API endpointleri tarafından uygulanır.

## 6. Security.API Giriş Noktası

Security API giriş noktası:

```text
src/Services/Security/Security.API/Program.cs
```

Başlatma sırasında yapılan işlemler:

1. Serilog yapılandırılır.
2. Application katmanı eklenir.
3. Infrastructure katmanı eklenir.
4. Rate limiting eklenir.
5. ProblemDetails exception handler eklenir.
6. OpenAPI eklenir.
7. Health checks tanımlanır.
8. Middleware pipeline kurulur.
9. Development ve IntegrationTests ortamlarında EF Core migration çalıştırılır ve seed yapılır.
10. Staging ortamında migration çalıştırılır.
11. Authentication ve Authorization etkinleştirilir.
12. Health ve API endpointleri map edilir.

### 6.1 Middleware Pipeline

Security.API içinde kullanılan middleware sırası özetle:

- Serilog request logging
- Exception handler
- Status code pages
- Correlation id middleware
- Log enrichment middleware
- Rate limiter
- Authentication
- Authorization
- Endpoint mapping

`CorrelationIdMiddleware`, `X-Correlation-Id` header'ını okur. Header yoksa `TraceIdentifier` kullanır. Response header'a aynı correlation id eklenir.

`LogEnrichmentMiddleware`, log context içine:

- correlation id,
- request path,
- HTTP method,
- user id,
- session id,
- access token jti

alanlarını ekler.

### 6.2 Health Check Endpointleri

Security.API iki health endpoint sunar:

| Endpoint | Amaç | Kontroller |
|---|---|---|
| `/health/live` | Process ayakta mı? | `self` |
| `/health/ready` | Kritik bağımlılıklar hazır mı? | PostgreSQL ve Redis |

Health response JSON olarak döner ve her entry için status, açıklama, süre ve data alanlarını içerir.

### 6.3 OpenAPI

Development ortamında:

```text
/openapi/v1.json
```

üzerinden OpenAPI dokümanı map edilir. Bearer security scheme transformer eklenmiştir.

## 7. Security.API Endpointleri

Security.API Minimal API endpointleri kullanır. Endpoint grupları:

- `/api/auth`
- `/api/mfa`
- `/api/sessions`
- `/api/users`
- `/api/test`
- `/health/live`
- `/health/ready`

### 7.1 Auth Endpointleri

Dosya:

```text
src/Services/Security/Security.API/Endpoints/AuthEndpoints.cs
```

| Method | Path | Açıklama | Auth | Rate Limit |
|---|---|---|---|---|
| POST | `/api/auth/register` | Yeni kullanıcı oluşturur | Hayır | Register |
| POST | `/api/auth/login` | E-posta/şifre ile login | Hayır | Login |
| POST | `/api/auth/refresh` | Refresh token döndürür/rotate eder | Hayır | Refresh |
| POST | `/api/auth/logout` | Mevcut session'ı kapatır | Evet | Logout |
| POST | `/api/auth/logout-all` | Kullanıcının tüm session'larını kapatır | Evet | Logout |
| POST | `/api/auth/forgot-password` | Şifre sıfırlama sürecini başlatır | Hayır | ForgotPassword |
| POST | `/api/auth/reset-password` | Şifre sıfırlama token'ı ile şifre değiştirir | Hayır | ResetPassword |
| POST | `/api/auth/verify-email` | E-posta doğrulama token'ını kullanır | Hayır | VerifyEmail |
| POST | `/api/auth/resend-verification` | Yeni doğrulama e-postası üretir | Hayır | ResendVerification |

Auth endpointleri HTTP request contract'larını Application command'larına dönüştürür. İş mantığı endpoint içinde değil, MediatR handler'larında bulunur. Sonuçlar `ApplicationResultMapper` ile HTTP response'a çevrilir.

### 7.2 MFA Endpointleri

Dosya:

```text
src/Services/Security/Security.API/Endpoints/MfaEndpoints.cs
```

| Method | Path | Açıklama | Auth |
|---|---|---|---|
| POST | `/api/mfa/setup/begin` | TOTP MFA kurulumunu başlatır | Evet |
| POST | `/api/mfa/setup/complete` | TOTP kodunu doğrular, MFA'yı aktif eder | Evet |
| POST | `/api/mfa/login/complete` | Login MFA challenge'ını tamamlar | Hayır |
| POST | `/api/mfa/disable` | MFA'yı TOTP veya recovery code ile kapatır | Evet |
| POST | `/api/mfa/recovery-codes/regenerate` | Recovery code listesini yeniler | Evet |

MFA login flow'unda normal login başarılı olduğunda kullanıcıda aktif MFA varsa access token hemen dönülmez. Bunun yerine kısa ömürlü challenge token üretilir. Kullanıcı `/api/mfa/login/complete` ile TOTP veya recovery code göndererek access token alır.

### 7.3 Session Endpointleri

Dosya:

```text
src/Services/Security/Security.API/Endpoints/SessionEndpoints.cs
```

| Method | Path | Açıklama | Auth |
|---|---|---|---|
| GET | `/api/sessions/` | Kullanıcının session listesini döndürür | Evet |
| DELETE | `/api/sessions/{id:guid}` | Belirli session'ı revoke eder | Evet |

Session listesinde current session bilgisi de işaretlenir. Bir session revoke edildiğinde refresh token'lar kapatılır ve Redis üzerinde session token invalidation kaydı tutulur.

### 7.4 User Endpointleri

Dosya:

```text
src/Services/Security/Security.API/Endpoints/UserEndpoints.cs
```

| Method | Path | Açıklama | Auth |
|---|---|---|---|
| GET | `/api/users/me` | Access token claim'lerinden mevcut kullanıcı bilgisini döndürür | Evet |

Response içinde kullanıcı id, e-posta, session id, access token jti ve permission claim'leri bulunur.

### 7.5 Test Endpointleri

Dosya:

```text
src/Services/Security/Security.API/Endpoints/TestEndpoints.cs
```

| Method | Path | Açıklama | Auth/Policy |
|---|---|---|---|
| GET | `/api/test/authenticated` | Kullanıcının authenticated olduğunu test eder | Auth |
| GET | `/api/test/users-read` | `users.read` permission policy'sini test eder | Auth + `users.read` |

Bu endpointler güvenlik ve yetkilendirme davranışını doğrulamak için kullanılır.

## 8. Transactions.WebAPI Detayları

Transactions API giriş noktası:

```text
src/Services/Transactions/Transactions.WebAPI/Program.cs
```

Bu servis:

- Controller desteği ekler.
- `InputsApi` adlı HttpClient tanımlar.
- Varsayılan CORS policy ile tüm origin/header/method değerlerine izin verir.
- HTTPS redirection kullanır.
- Controller route'larını map eder.

`InputsApi:BaseUrl` varsayılan olarak:

```text
http://localhost:8000
```

Docker Compose ortamında:

```text
http://host.docker.internal:8000
```

olarak set edilir.

### 8.1 Transactions Controller

Dosya:

```text
src/Services/Transactions/Transactions.WebAPI/Controllers/TransactionsController.cs
```

Base route:

```text
/api/transactions
```

| Method | Path | Açıklama |
|---|---|---|
| POST | `/api/transactions/file-input` | Multipart dosyayı ve `user_id` bilgisini Inputs API `/v1/inputs` endpoint'ine iletir |
| POST | `/api/transactions/file-extract` | Dosya tipine göre extraction endpoint'ini çağırır, sonra normalization çağırır |
| POST | `/api/transactions/ai-save` | AI analiz ve kayıt isteğini `/v1/ai/analyze-and-save` endpoint'ine iletir |
| POST | `/api/transactions/ai-chat` | AI chat isteğini `/v1/ai/chat` endpoint'ine iletir |

### 8.2 File Input Akışı

`file-input` endpoint'i:

- Form-data üzerinden `user_id` alır.
- `IFormFile file` alır.
- `user_id` boşsa `400 Bad Request` döner.
- Dosya yoksa veya boyutu sıfırsa `400 Bad Request` döner.
- Multipart form oluşturup `InputsApi` servisine `/v1/inputs` path'i ile POST eder.
- Inputs API response body JSON parse edilebilirse JSON olarak, edilemezse string olarak döndürülür.
- Response içine dosya metadata'sı eklenir:
  - fileName
  - contentType
  - size
  - uploadedAt

Request size limiti 50 MB'dir.

### 8.3 File Extract Akışı

`file-extract` endpoint'i body içinde:

```text
input_id
file_name
```

alanlarını bekler.

`file_name` değerine göre extraction tipi belirlenir:

| `file_name` | Extraction tipi |
|---|---|
| `scanned_pdf` | `image` |
| `real_pdf` | `pdf` |
| `camera_photo` | `image` |
| `screenshot` | `image` |

Geçersiz `file_name` için `400 Bad Request` döner.

Akış:

1. `/v1/extractions/{file_type}/{input_id}` çağrılır.
2. Ardından `/v1/normalizations/{input_id}` çağrılır.
3. Son response olarak normalization sonucu döndürülür.

### 8.4 AI Save ve AI Chat

`ai-save`, `AISaveRequest` modelini olduğu gibi Inputs API `/v1/ai/analyze-and-save` endpoint'ine JSON olarak gönderir.

`ai-chat`, `AIChatRequest` modelini Inputs API `/v1/ai/chat` endpoint'ine JSON olarak gönderir.

### 8.5 Transactions Request Modelleri

`AIChatRequest`:

```text
analysis_id
question
```

`FileExtractRequest`:

```text
input_id
file_name
```

`AISaveRequest`:

```text
input_id
status
result
scores
historical_transactions
question
purchase_scenario
use_llm
```

`AISaveResult` içinde transaction listesi ve summary bulunur. Transaction modeli tarih, açıklama, merchant, amount, currency ve direction alanlarını taşır.

## 9. Security.Application Katmanı

Application katmanı şu sorumluluklara sahiptir:

- HTTP'den bağımsız use-case'leri modellemek.
- Command/query ve handler'ları MediatR ile çalıştırmak.
- FluentValidation ile input doğrulamak.
- Domain modellerini repository arayüzleri üzerinden kullanmak.
- Dış teknoloji bağımlılıklarını abstraction olarak tanımlamak.
- İş sonucunu `Result` / `Result<T>` tipi ile ifade etmek.

Giriş noktası:

```text
src/Services/Security/Security.Application/DependencyInjection.cs
```

Burada:

- MediatR kayıtları yapılır.
- `ValidationBehavior<,>` pipeline'a eklenir.
- FluentValidation validator'ları assembly'den yüklenir.

### 9.1 Result Modeli

Application katmanı exception yerine çoğu beklenen iş hatasını `Result` ile döndürür.

`Result`:

- `IsSuccess`
- `IsFailure`
- `Error`

alanlarına sahiptir.

`Result<T>` başarılı sonuçlarda `Value` taşır. Failure durumda `Value` okunursa exception fırlatır.

### 9.2 Validation Pipeline

`ValidationBehavior<TRequest,TResponse>`:

- Request için validator yoksa handler'a devam eder.
- Validator varsa tüm validasyonları çalıştırır.
- Hataları property adına göre gruplayıp camelCase yapar.
- `validation.invalid` kodlu `Error` üretir.
- Response tipi `Result` veya `Result<T>` ise failure response oluşturur.

Bu sayede endpointlerin çoğunda manuel validasyon kodu bulunmaz.

### 9.3 Auth Command Akışları

#### Register

Handler:

```text
Security.Application/Auth/Register/RegisterCommandHandler.cs
```

Akış:

1. E-posta normalize edilir.
2. Aynı normalized email var mı kontrol edilir.
3. Yeni `User` oluşturulur.
4. Şifre hash'lenir.
5. 24 saat geçerli e-posta doğrulama token'ı üretilir.
6. Token hash'i veritabanına kaydedilir.
7. UserRegistered ve EmailVerificationRequested audit log kayıtları yazılır.
8. Unit of Work ile kayıtlar commit edilir.
9. Resend üzerinden e-posta doğrulama maili gönderilmeye çalışılır.
10. E-posta gönderimi hata verse bile kullanıcı oluşturma işlemi geri alınmaz; hata loglanır.

#### Login

Handler:

```text
Security.Application/Auth/Login/LoginCommandHandler.cs
```

Akış:

1. E-posta normalize edilir.
2. Kullanıcı bulunur.
3. Kullanıcı aktif mi kontrol edilir.
4. Şifre doğrulanır.
5. Login başarısızsa audit log yazılır.
6. Başarılıysa `LastLoginAtUtc` güncellenir.
7. Yeni `RefreshSession` oluşturulur.
8. Yeni refresh token üretilir ve hash'i session içine eklenir.
9. Kullanıcıda aktif ve doğrulanmış MFA varsa:
   - session kaydedilir,
   - 5 dakika geçerli MFA challenge token oluşturulur,
   - access token dönülmez,
   - `RequiresMfa = true` döner.
10. MFA yoksa:
   - kullanıcı permission'ları okunur,
   - access token oluşturulur,
   - refresh token plaintext olarak response'a eklenir,
   - LoginSucceeded audit log yazılır.

#### Refresh Token

Handler:

```text
Security.Application/Auth/Refresh/RefreshTokenCommandHandler.cs
```

Akış:

1. Gelen refresh token hash'lenir.
2. Hash'e göre session bulunur.
3. Token session'a bağlı mı kontrol edilir.
4. Session revoked ise hata döner.
5. Token revoked veya consumed ise reuse detection çalışır:
   - session revoke edilir,
   - RefreshReuseDetected audit log yazılır,
   - `auth.refresh_token_reuse_detected` döner.
6. Token expired ise hata döner.
7. Kullanıcı aktif mi kontrol edilir.
8. Eski refresh token consumed olarak işaretlenir.
9. Yeni access token üretilir.
10. Yeni refresh token üretilip session'a eklenir.
11. RefreshSucceeded audit log yazılır.

Bu mekanizma refresh token rotation ve reuse detection uygular.

#### Logout

`LogoutCommandHandler`:

- Mevcut session'ı bulur.
- Session kullanıcıya ait değilse hata döner.
- Session'ı revoke eder.
- Mevcut access token jti değerini Redis'te revoke eder.
- Session bazlı access token invalidation kaydı oluşturur.
- LogoutCurrentSession audit log yazar.

#### Logout All

`LogoutAllCommandHandler`:

- Kullanıcının tüm session'larını bulur.
- Hepsini revoke eder.
- Mevcut access token jti değerini Redis'te revoke eder.
- Kullanıcı bazlı token invalidation kaydı oluşturur.
- LogoutAllSessions audit log yazar.

### 9.4 Şifre Sıfırlama Akışı

#### Forgot Password

Handler:

```text
Security.Application/Auth/PasswordReset/ForgotPassword/ForgotPasswordCommandHandler.cs
```

Akış:

- E-posta normalize edilir.
- Kullanıcı varsa ve aktifse 30 dakika geçerli password reset token oluşturulur.
- Token hash'i veritabanına kaydedilir.
- PasswordResetRequested audit log yazılır.
- E-posta gönderilmeye çalışılır.
- Kullanıcı yoksa bile aynı genel mesaj dönülür.

Bu yaklaşım hesap var/yok bilgisinin dışarı sızmasını azaltır.

#### Reset Password

Handler:

```text
Security.Application/Auth/PasswordReset/ResetPassword/ResetPasswordCommandHandler.cs
```

Akış:

1. Token hash'lenir.
2. Token bulunur.
3. Kullanılmış mı, süresi geçmiş mi kontrol edilir.
4. Kullanıcı aktif mi kontrol edilir.
5. Şifre hash'i güncellenir.
6. Reset token used olarak işaretlenir.
7. Kullanıcının tüm refresh session'ları revoke edilir.
8. Redis'te kullanıcı bazlı access token invalidation kaydı oluşturulur.
9. PasswordResetCompleted audit log yazılır.

Şifre sıfırlanınca tüm oturumlar kapatılır.

### 9.5 E-posta Doğrulama Akışı

#### Verify Email

Handler:

```text
Security.Application/Auth/EmailVerification/VerifyEmail/VerifyEmailCommandHandler.cs
```

Akış:

- Token hash'lenir.
- Token var mı, kullanılmış mı, süresi geçmiş mi kontrol edilir.
- Kullanıcı aktif mi kontrol edilir.
- Kullanıcının e-postası zaten doğrulanmış mı kontrol edilir.
- User `EmailVerified = true` yapılır.
- Token used olarak işaretlenir.
- EmailVerified audit log yazılır.

#### Resend Verification

Handler:

```text
Security.Application/Auth/EmailVerification/ResendVerification/ResendVerificationCommandHandler.cs
```

Akış:

- Kullanıcı varsa, aktifse ve e-postası doğrulanmamışsa yeni 24 saatlik verification token üretilir.
- Audit log yazılır.
- E-posta gönderilmeye çalışılır.
- Kullanıcı yoksa veya zaten doğrulanmışsa yine genel başarılı mesaj dönülür.

### 9.6 MFA Akışları

#### Begin MFA Setup

Handler:

```text
Security.Application/Auth/Mfa/BeginSetup/BeginMfaSetupCommandHandler.cs
```

Akış:

- TOTP secret üretilir.
- Secret Data Protection ile şifrelenir.
- Secret SHA-256 ile hash'lenir.
- Kullanıcı için MFA kaydı yoksa oluşturulur, varsa pending secret resetlenir.
- OTP Auth URI oluşturulur.
- MfaSetupStarted audit log yazılır.
- Response içinde manual entry key ve otpauth URI döner.

Issuer sabittir:

```text
SecurityService
```

#### Complete MFA Setup

Handler:

```text
Security.Application/Auth/Mfa/CompleteSetup/CompleteMfaSetupCommandHandler.cs
```

Akış:

- Kullanıcının MFA method'u bulunur.
- Secret decrypt edilir.
- Girilen TOTP kod doğrulanır.
- MFA verified ve enabled yapılır.
- 10 recovery code üretilir.
- Recovery code hash'leri veritabanına kaydedilir.
- MfaEnabled audit log yazılır.
- Plaintext recovery code listesi response'ta döner.

#### Complete MFA Login

Handler:

```text
Security.Application/Auth/Mfa/VerifyLogin/CompleteMfaLoginCommandHandler.cs
```

Akış:

- Challenge token Data Protection ile validate edilir.
- Token süresi kontrol edilir.
- User, MFA method ve session bulunur.
- TOTP code veya recovery code doğrulanır.
- Recovery code kullanıldıysa used işaretlenir.
- Permission'lar okunur.
- Access token üretilir.
- Session'daki aktif refresh token expiry bilgisi alınır.
- Login response döner.

Challenge token payload içinde refresh token plaintext değeri de korunur. Kod içinde bu noktaya dair Türkçe not vardır: plaintext refresh token artık doğrudan elde olmadığı için challenge payload'daki refresh token response'a eklenir.

#### Disable MFA

Handler:

```text
Security.Application/Auth/Mfa/Disable/DisableMfaCommandHandler.cs
```

Akış:

- MFA enabled/verified mı kontrol edilir.
- TOTP veya recovery code ile doğrulama yapılır.
- MFA disable edilir.
- Recovery code'lar temizlenir.
- MfaDisabled audit log yazılır.

#### Regenerate Recovery Codes

Handler:

```text
Security.Application/Auth/Mfa/RecoveryCodes/RegenerateRecoveryCodesCommandHandler.cs
```

Akış:

- MFA enabled/verified mı kontrol edilir.
- TOTP code doğrulanır.
- Eski recovery code'lar silinir.
- 10 yeni recovery code üretilir ve hash'leri kaydedilir.
- RecoveryCodesRegenerated audit log yazılır.
- Plaintext yeni recovery code'lar response'ta döner.

### 9.7 Session Akışları

#### Get My Sessions

Handler:

```text
Security.Application/Sessions/GetMySessions/GetMySessionsQueryHandler.cs
```

Kullanıcının session'larını oluşturulma zamanına göre ters sıralar ve current session bilgisini işaretler.

#### Revoke Session

Handler:

```text
Security.Application/Sessions/RevokeSession/RevokeSessionCommandHandler.cs
```

Akış:

- Session bulunur.
- Kullanıcıya ait olup olmadığı kontrol edilir.
- Session revoke edilir.
- Redis'e session invalidation kaydı yazılır.
- Eğer revoke edilen session mevcut session ise mevcut access token jti ayrıca revoke edilir.
- SessionRevoked audit log yazılır.

## 10. Validasyon Kuralları

FluentValidation ile tanımlanan önemli kurallar:

### 10.1 E-posta

Register, Login, ForgotPassword ve ResendVerification:

- boş olamaz,
- maksimum 320 karakter,
- e-posta formatında olmalı.

### 10.2 Şifre

Register ve ResetPassword:

- boş olamaz,
- minimum 12 karakter,
- maksimum 200 karakter,
- en az bir büyük harf,
- en az bir küçük harf,
- en az bir rakam,
- en az bir alfanümerik olmayan karakter içermeli.

Login password:

- boş olamaz,
- maksimum 200 karakter.

### 10.3 Token Alanları

Refresh token, password reset token ve verification token:

- boş olamaz,
- maksimum 2048 karakter.

Access token jti:

- boş olamaz,
- maksimum 200 karakter.

### 10.4 MFA Kodları

TOTP code:

- boş olamaz,
- 6-8 karakter uzunlukta,
- sadece rakam.

Recovery code:

- minimum 8,
- maksimum 64 karakter.

Disable MFA ve Complete MFA Login işlemlerinde TOTP code veya recovery code alanlarından en az biri verilmelidir.

## 11. Security.Domain Katmanı

Domain katmanı iş kurallarını taşıyan entity ve aggregate'lerden oluşur.

### 11.1 Base Tipler

- `Entity`: Temel entity soyutlaması.
- `AggregateRoot`: Domain events listesi taşıyan aggregate root tabanı.
- `IDomainEvent`: Domain event marker interface.
- `Guard`: Boş Guid, boş string, default tarih ve koşul kontrolleri için yardımcı sınıf.
- `DomainException`: Domain ihlalleri için exception tipi.

### 11.2 User

Dosya:

```text
Security.Domain/Users/User.cs
```

Alanlar:

- `Id`
- `Email`
- `NormalizedEmail`
- `PasswordHash`
- `EmailVerified`
- `IsActive`
- `CreatedAtUtc`
- `LastLoginAtUtc`
- `Roles`

Davranışlar:

- e-postayı doğrulanmış işaretleme,
- login zamanını güncelleme,
- şifre hash'ini değiştirme,
- kullanıcıyı aktif/pasif yapma,
- rol ekleme/çıkarma.

### 11.3 Role ve Permission

`Role`:

- `Id`
- `Name`
- `NormalizedName`
- `Permissions`

Role, permission ekleyip çıkarabilir.

`Permission`:

- `Id`
- `Code`

Permission kodları:

```text
users.read
users.manage
roles.read
roles.manage
permissions.read
permissions.manage
sessions.read
sessions.manage
```

### 11.4 RefreshSession ve RefreshToken

`RefreshSession`:

- Kullanıcıya ait refresh token grubunu temsil eder.
- Device name ve IP address tutar.
- Revoked durumunu ve revoked zamanını taşır.
- İçindeki tüm token'ları revoke edebilir.
- En son kullanılabilir token'ı döndürebilir.

`RefreshToken`:

- Hash olarak saklanır.
- Expiry, created, consumed, revoked bilgileri vardır.
- Consume ve revoke davranışlarına sahiptir.
- Expired/usable kontrolü yapar.

### 11.5 E-posta ve Şifre Tokenları

`EmailVerificationToken` ve `PasswordResetToken` benzer yapıya sahiptir:

- `Id`
- `UserId`
- `TokenHash`
- `ExpiresAtUtc`
- `CreatedAtUtc`
- `Used`
- `UsedAtUtc`

Her iki token tipi de:

- used olarak işaretlenebilir,
- expired kontrolü yapabilir,
- usable kontrolü yapabilir.

Plaintext token veritabanına yazılmaz; SHA-256 hash'i saklanır.

### 11.6 MFA Domain Modeli

`MfaMethod`:

- Kullanıcıya ait MFA kaydını temsil eder.
- Şu an sadece TOTP desteklenir.
- Secret hash ve encrypted secret saklar.
- Verified ve enabled durumlarını tutar.
- Recovery code listesine sahiptir.

`RecoveryCode`:

- Tek kullanımlık MFA kurtarma kodunun hash'ini taşır.
- Used durumunu ve used zamanını saklar.

### 11.7 AuditLog

Audit log entity'si:

- `Id`
- `UserId`
- `ActionType`
- `IpAddress`
- `UserAgent`
- `CorrelationId`
- `PayloadJson`
- `CreatedAtUtc`

Audit action tipleri:

- user registration,
- login succeeded/failed,
- refresh succeeded/failed/reuse detected,
- logout current/all,
- session revoked,
- email verification requested/verified,
- password reset requested/completed,
- role/permission değişiklikleri,
- MFA setup/enabled/login/recovery/disabled,
- recovery codes regenerated.

## 12. Security.Infrastructure Katmanı

Infrastructure katmanı gerçek teknoloji entegrasyonlarını sağlar.

Giriş noktası:

```text
src/Services/Security/Security.Infrastructure/DependencyInjection.cs
```

### 12.1 PostgreSQL ve EF Core

`SecurityDbContext`:

```text
src/Services/Security/Security.Infrastructure/Persistence/SecurityDbContext.cs
```

DbSet'ler:

- `Users`
- `UserRoles`
- `Roles`
- `Permissions`
- `RolePermissions`
- `RefreshSessions`
- `RefreshTokens`
- `EmailVerificationTokens`
- `PasswordResetTokens`
- `AuditLogs`
- `MfaMethods`
- `RecoveryCodes`

Varsayılan schema:

```text
security
```

Migration history table:

```text
security.__ef_migrations_history
```

OpenIddict EF entegrasyonu da DbContext üzerinde etkinleştirilmiştir.

### 12.2 EF Core Konfigürasyonları

Önemli tablo adları:

| Entity | Tablo |
|---|---|
| User | `users` |
| UserRole | `user_roles` |
| Role | `roles` |
| Permission | `permissions` |
| RolePermission | `role_permissions` |
| RefreshSession | `refresh_sessions` |
| RefreshToken | `refresh_tokens` |
| EmailVerificationToken | `email_verification_tokens` |
| PasswordResetToken | `password_reset_tokens` |
| AuditLog | `audit_logs` |
| MfaMethod | `mfa_methods` |
| RecoveryCode | `recovery_codes` |

Önemli indexler:

- `users.normalized_email` unique.
- `roles.normalized_name` unique.
- `permissions.code` unique.
- refresh token hash unique.
- e-posta verification token hash unique.
- password reset token hash unique.
- MFA method user id unique.
- audit log user/time indexleri.

### 12.3 Repository'ler

Application katmanındaki persistence arayüzlerinin implementasyonları:

- `UserRepository`
- `RoleRepository`
- `RefreshSessionRepository`
- `AuditLogRepository`
- `EmailVerificationTokenRepository`
- `PasswordResetTokenRepository`
- `MfaMethodRepository`
- `UnitOfWork`

`UnitOfWork`, EF Core `SaveChangesAsync` çağrısını merkezi hale getirir.

### 12.4 JWT Üretimi ve Doğrulaması

JWT üretici:

```text
Security.Infrastructure/Security/Jwt/JwtTokenGenerator.cs
```

JWT claim'leri:

- subject/user id,
- email,
- jwt id,
- issued at,
- session id,
- permission claim'leri.

Access token lifetime config üzerinden gelir. Development ortamında 15 dakikadır.

JWT doğrulama Security.Infrastructure içinde yapılandırılır:

- issuer doğrulanır,
- audience doğrulanır,
- signing key doğrulanır,
- lifetime doğrulanır,
- 30 saniye clock skew vardır,
- inbound claim mapping kapalıdır,
- token validated event'inde Redis revocation kontrolleri yapılır.

Token validated sırasında kontrol edilenler:

- jti var mı?
- subject Guid mi?
- iat var mı?
- jti Redis'te revoke edilmiş mi?
- user-level invalidation var mı?
- session-level invalidation var mı?

Challenge ve forbidden response'ları RFC7807 benzeri ProblemDetails JSON döndürür.

### 12.5 Redis Access Token Revocation

Implementasyon:

```text
Security.Infrastructure/Security/Redis/RedisAccessTokenRevocationStore.cs
```

Redis üç tip invalidation tutar:

1. Tekil access token jti revoke kaydı.
2. Kullanıcı bazlı token invalidation kaydı.
3. Session bazlı token invalidation kaydı.

Key prefix ayarları:

```text
RedisRevocation:InstanceName
RedisRevocation:AccessTokenRevocationPrefix
RedisRevocation:UserInvalidationPrefix
RedisRevocation:SessionInvalidationPrefix
```

Development default:

```text
security:revoked:access:jti:{jti}
security:revoked:access:user:{userId}
security:revoked:access:session:{sessionId}
```

TTL, token expiry veya retention config'ine göre hesaplanır.

### 12.6 Password Hashing

Password hashing:

```text
Security.Infrastructure/Security/PasswordHasher.cs
```

ASP.NET Core Identity `PasswordHasher<object>` kullanılır. Verify sırasında `Success` ve `SuccessRehashNeeded` başarılı kabul edilir.

### 12.7 Token Üreticileri

Refresh token, e-posta verification token ve password reset token üreticileri:

- 64 byte cryptographic random veri üretir.
- Base64 plaintext token oluşturur.
- SHA-256 hash alır.
- Plaintext sadece kullanıcıya/response'a gider.
- Hash veritabanında saklanır.

### 12.8 MFA Altyapısı

`TotpService`:

- 20 byte random TOTP secret üretir.
- Base32 formatına çevirir.
- otpauth URI oluşturur.
- Otp.NET ile TOTP doğrular.

`TotpSecretProtector`:

- Data Protection ile secret encrypt/decrypt yapar.
- Secret hash'ini SHA-256 ile üretir.

`RecoveryCodeService`:

- Varsayılan 10 recovery code üretir.
- Her code için 8 byte random veri alır.
- Hex string'in ilk 10 karakterini kullanır.
- Code hash'ini SHA-256 ile üretir.

`MfaChallengeTokenService`:

- Data Protection ile MFA challenge payload'ını protect eder.
- Payload içinde user id, session id, refresh token ve expiry bulunur.
- Validate sırasında decrypt/deserialize eder; hata durumunda null döner.

### 12.9 E-posta Altyapısı

E-posta gönderimi:

```text
Security.Infrastructure/Email/ResendEmailSender.cs
```

Resend client kullanılır. Config:

```text
Resend:ApiKey
Resend:FromEmail
Resend:FromName
Resend:FrontendBaseUrl
Resend:VerifyEmailPath
Resend:ResetPasswordPath
```

`Resend:ApiKey` eksikse Infrastructure dependency injection sırasında exception fırlatılır.

Register, resend verification ve forgot password akışlarında e-posta gönderimi denenir. Gönderim hataları loglanır fakat ana transaction tamamlandıysa kullanıcıya genel başarı cevabı dönebilir.

### 12.10 Identity Seed

Seed sınıfı:

```text
Security.Infrastructure/Persistence/Seed/IdentitySeeder.cs
```

Development ve IntegrationTests ortamlarında migration sonrası çalışır.

Seed işlemleri:

1. Permission kodlarını ekler.
2. `Admin` rolünü oluşturur.
3. Tüm permission'ları Admin rolüne atar.
4. Config'teki admin kullanıcıyı oluşturur.
5. Admin kullanıcının e-postasını verified yapar.
6. Admin rolünü kullanıcıya atar.

Development default admin:

```text
AdminEmail: admin@local
AdminPassword: DummyPw123!
```

Production ortamında seed, `AllowInProduction` false ise engellenir.

## 13. ProblemDetails ve Hata Mapping

Security.API beklenen application hatalarını HTTP response'a map eder.

Mapper:

```text
Security.API/Common/ErrorMapping/ApplicationResultMapper.cs
```

Örnek mapping'ler:

| Error code | HTTP status | Başlık |
|---|---:|---|
| `validation.invalid` | 400 | Validation failed |
| `auth.invalid_credentials` | 401 | Authentication failed |
| `auth.invalid_refresh_token` | 401 | Invalid refresh token |
| `auth.expired_refresh_token` | 401 | Expired refresh token |
| `auth.session_revoked` | 401 | Session revoked |
| `auth.refresh_token_reuse_detected` | 401 | Refresh token reuse detected |
| `auth.invalid_session` | 400 | Invalid session |
| `auth.session_not_found` | 404 | Session not found |
| `auth.user_inactive` | 403 | User inactive |
| `auth.user_already_exists` | 409 | Conflict |
| `auth.invalid_password_reset_token` | 400 | Invalid password reset token |
| `auth.expired_password_reset_token` | 400 | Expired password reset token |
| `auth.invalid_email_verification_token` | 400 | Invalid email verification token |
| `auth.email_already_verified` | 400 | Email already verified |
| `auth.mfa_not_initialized` | 400 | MFA not initialized |
| `auth.invalid_mfa_code` | 400 | Invalid MFA code |
| `auth.mfa_not_enabled` | 400 | MFA not enabled |
| `auth.invalid_mfa_challenge` | 401 | Invalid MFA challenge |

ProblemDetails response'larına correlation id eklenir:

```text
correlationId
```

Validation problem response'larında `errors` sözlüğü kullanılır.

## 14. Rate Limiting

Security.API özel rate limit policy'leri kullanır.

Policy adları:

```text
rate-limit:register
rate-limit:login
rate-limit:refresh
rate-limit:logout
rate-limit:sessions
rate-limit:forgot-password
rate-limit:reset-password
rate-limit:verify-email
rate-limit:resend-verification
```

Policy'ler fixed window limiter kullanır.

Partition stratejisi:

- Register, login, refresh, forgot password, reset password, verify email ve resend verification IP bazlıdır.
- Logout ve sessions authenticated user veya IP bazlıdır.

Development config limitleri:

| Policy | PermitLimit | WindowSeconds |
|---|---:|---:|
| Register | 3 | 60 |
| Login | 5 | 60 |
| Refresh | 10 | 60 |
| Logout | 10 | 60 |
| Sessions | 20 | 60 |
| ForgotPassword | 3 | 60 |
| ResetPassword | 5 | 60 |
| VerifyEmail | 10 | 60 |
| ResendVerification | 3 | 60 |

Rate limit aşılırsa `429 Too Many Requests` ve ProblemDetails response döner. Eğer limiter metadata içinde retry-after üretirse response header'a `Retry-After` eklenir.

Gateway tarafında ayrıca `standard` adlı fixed window limiter vardır:

- 100 request
- 1 dakika
- queue yok

## 15. Yetkilendirme ve Permission Modeli

Security.Infrastructure içinde ASP.NET Authorization policy'leri permission kodlarına göre tanımlanır:

- `users.read`
- `users.manage`
- `roles.read`
- `roles.manage`
- `permissions.read`
- `permissions.manage`
- `sessions.read`
- `sessions.manage`

`PermissionAuthorizationHandler`, access token claim'leri içindeki permission değerlerini kontrol eder. Test endpoint'i `/api/test/users-read`, `users.read` policy'si ile bunu doğrular.

JWT üretimi sırasında kullanıcının role-permission ilişkilerinden permission kodları okunur ve token'a claim olarak eklenir.

## 16. Konfigürasyon Dosyaları

### 16.1 Security.API appsettings

Base config:

```text
src/Services/Security/Security.API/appsettings.json
```

Temel logging ve allowed hosts ayarları içerir.

Development config:

```text
src/Services/Security/Security.API/appsettings.Development.json
```

İçerdiği bölümler:

- Serilog
- Resend
- ConnectionStrings
- Jwt
- IdentitySeed
- RedisRevocation
- RateLimiting
- SecurityTokenInvalidation

Önemli değerler:

```text
ConnectionStrings:Postgres = Host=localhost;Port=5432;Database=kemal_db;Username=kemal;Password=kemal1234;
ConnectionStrings:Redis = localhost:6379
Jwt:Issuer = SecurityService
Jwt:Audience = SecurityService.Clients
Jwt:AccessTokenLifetimeMinutes = 15
```

### 16.2 Gateway appsettings

Base config:

```text
src/ApiGateways/FinanceAnalysis.ApiGateway/appsettings.json
```

Development config:

```text
src/ApiGateways/FinanceAnalysis.ApiGateway/appsettings.Development.json
```

İçerdiği bölümler:

- Logging
- Jwt
- Cors
- ReverseProxy

### 16.3 Transactions appsettings

Base config:

```text
src/Services/Transactions/Transactions.WebAPI/appsettings.json
```

Önemli bölüm:

```text
InputsApi:BaseUrl = http://localhost:8000
```

Development config sadece logging ayarlarını override eder.

## 17. Docker ve Container Yapısı

Ana compose dosyası:

```text
docker-compose.yml
```

Kullanılan volume'ler:

- `kemal_db_data`
- `security_redis_data`
- `pgadmin_data`

Kullanılan network:

```text
finance-network
```

### 17.1 Security API Container

Build:

```text
src/Services/Security/Security.API/Dockerfile
```

Environment:

```text
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__Postgres=...
ConnectionStrings__Redis=security-redis:6379
Resend__ApiKey=${RESEND_API_KEY}
Resend__FromEmail=${RESEND_FROM_EMAIL}
```

Security API, PostgreSQL ve Redis'e bağlıdır.

### 17.2 Transactions API Container

Build:

```text
src/Services/Transactions/Transactions.WebAPI/Dockerfile
```

Environment:

```text
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
InputsApi__BaseUrl=http://host.docker.internal:8000
```

### 17.3 Gateway Container

Build:

```text
src/ApiGateways/FinanceAnalysis.ApiGateway/Dockerfile
```

Environment ile YARP destination adresleri override edilir:

```text
ReverseProxy__Clusters__security-cluster__Destinations__security-api__Address=http://security-api:8080/
ReverseProxy__Clusters__transactions-cluster__Destinations__transactions-api__Address=http://transactions-api:8080/
```

Gateway, Security API ve Transactions API servislerine bağlıdır.

## 18. Veritabanı Migration'ları

Migration dosyaları:

```text
src/Services/Security/Security.Infrastructure/Persistence/Migrations
```

Mevcut migration'lar:

- `20260331134159_InitialSecuritySchema`
- `20260602124242_AddMfaTotpSupport`

İlk migration security schema, kullanıcı/rol/izin/session/token/audit tablolarını oluşturur. İkinci migration TOTP MFA desteği için MFA method ve recovery code yapılarını ekler.

Security.API başlarken:

- Development ortamında migration çalışır ve seed yapılır.
- IntegrationTests ortamında migration çalışır ve seed yapılır.
- Staging ortamında migration çalışır.
- Production için otomatik migration yolu kodda açık değildir.

## 19. Test Yapısı

### 19.1 Security.IntegrationTests

Proje:

```text
tests/Security.IntegrationTests/Security.IntegrationTests.csproj
```

Kullandığı paketler:

- `Microsoft.AspNetCore.Mvc.Testing`
- `Testcontainers.PostgreSql`
- `Testcontainers.Redis`
- `FluentAssertions`
- `Otp.NET`
- `xUnit`

Önemli altyapı dosyaları:

- `CustomWebApplicationFactory`
- `IntegrationTestFixture`
- `TestAuthClient`
- `HardeningTestClient`
- `MfaTestCodeGenerator`
- `TestJson`

Test kategorileri:

| Klasör | Kapsam |
|---|---|
| `Tests/Auth` | Register, login, refresh, logout, e-posta doğrulama, password reset, revoked token |
| `Tests/Sessions` | Session listeleme ve revoke |
| `Tests/RateLimiting` | Rate limit davranışları |
| `Tests/Health` | Health check ve correlation id |
| `Tests/Hardening` | MFA, logout-all, password reset sonrası session invalidation, tek session revoke hardening |

Integration testlerde Security.API gerçek HTTP pipeline ile çalıştırılır. PostgreSQL ve Redis container'ları test için ayağa kaldırılır.

### 19.2 Security.ArchitectureTests

Proje:

```text
tests/Security.ArchitectureTests/Security.ArchitectureTests.csproj
```

NetArchTest.Rules paketi eklenmiştir. Bu proje katman bağımlılıklarını veya mimari kuralları test etmek için hazırlanmıştır.

### 19.3 Security.UnitTests

Proje:

```text
tests/Security.UnitTests/Security.UnitTests.csproj
```

xUnit altyapısı eklenmiştir. Mevcut dosya taramasında unit test kaynak dosyası görünmemektedir; proje iskelet halinde duruyor olabilir.

## 20. Güvenlik Tasarımı

Projede dikkat çeken güvenlik mekanizmaları:

### 20.1 Refresh Token Rotation

Her refresh işleminde:

- eski refresh token consumed yapılır,
- yeni refresh token üretilir,
- yeni token hash'i aynı session'a eklenir.

Consumed veya revoked token tekrar kullanılırsa reuse detection çalışır ve session revoke edilir.

### 20.2 Access Token Revocation

JWT normalde stateless olmasına rağmen Redis ile üç seviyeli invalidation yapılır:

- tekil jti revocation,
- session-level invalidation,
- user-level invalidation.

Logout, logout-all, session revoke ve password reset gibi olaylarda Redis kayıtları yazılır. Token validation sırasında bu kayıtlar kontrol edilir.

### 20.3 Password Reset Hardening

Şifre sıfırlama tamamlandığında:

- kullanıcının şifresi değişir,
- password reset token used yapılır,
- tüm refresh session'lar revoke edilir,
- user-level access token invalidation yapılır.

Bu sayede eski access token ve refresh token'lar kullanılamaz hale gelir.

### 20.4 MFA

MFA TOTP tabanlıdır:

- Secret encrypted saklanır.
- Secret hash ayrıca saklanır.
- Setup completion için TOTP doğrulaması gerekir.
- Recovery code'lar tek kullanımlıktır ve hash olarak saklanır.
- Login sırasında MFA aktifse access token verilmeden challenge flow'a geçilir.

### 20.5 Audit Logging

Kritik güvenlik olayları audit log olarak yazılır. Audit payload JSONB olarak saklanır. IP address, user agent ve correlation id bilgileri audit kaydına dahil edilir.

### 20.6 Generic Responses

Forgot password ve resend verification gibi hesap varlığını sızdırabilecek uçlarda genel mesaj döndürülür. Kullanıcı yoksa bile response başarılı görünebilir.

### 20.7 Rate Limiting

Register, login, forgot password, reset password ve verification gibi brute force veya abuse riski olan uçlara rate limit uygulanır.

## 21. Gözlemlenen Mimari Kararlar

### 21.1 Clean Architecture Yaklaşımı

Security servisi katmanlar arası bağımlılığı kontrollü tutar:

- Domain hiçbir dış pakete bağımlı değildir.
- Application, Domain'e ve abstraction'lara dayanır.
- Infrastructure, Application arayüzlerini uygular.
- API, Application ve Infrastructure'ı composition root olarak birleştirir.

### 21.2 MediatR ile Use Case Ayrımı

Endpointler ince tutulmuştur. Her endpoint request'i ilgili command/query'ye çevirir. İş akışı handler içinde yürür.

### 21.3 Result Tabanlı Hata Yönetimi

Beklenen iş hataları exception olarak değil `Result` failure olarak döner. API katmanı bu hataları merkezi mapper ile HTTP response'a çevirir.

### 21.4 Infrastructure Soyutlamaları

JWT, token generation, password hashing, e-posta gönderimi, request context, audit factory, repository ve unit-of-work bağımlılıkları arayüzlerle soyutlanmıştır.

### 21.5 Security.API ve Gateway JWT Ayarları

Security.API JWT doğrulamasını daha kapsamlı yapar; Redis revocation kontrolleri dahil edilmiştir. Gateway tarafında ise standart JWT validation ve gateway route authorization vardır.

## 22. Geliştirme ve Çalıştırma Notları

### 22.1 Docker Compose ile Çalıştırma

Gerekli environment değişkenleri `.env` veya shell üzerinden sağlanmalıdır:

```text
REDIS_PORT
POSTGRES_USER
POSTGRES_PASSWORD
POSTGRES_DB
POSTGRES_PORT
PGADMIN_DEFAULT_EMAIL
PGADMIN_DEFAULT_PASSWORD
PGADMIN_PORT
RESEND_API_KEY
RESEND_FROM_EMAIL
```

Çalıştırma komutu:

```bash
docker compose up --build
```

Gateway:

```text
http://localhost:8080
```

Security API doğrudan:

```text
http://localhost:7001
```

Transactions API doğrudan:

```text
http://localhost:7002
```

### 22.2 Lokal Security.API Çalıştırma

PostgreSQL ve Redis lokal çalışıyorsa:

```bash
dotnet run --project src/Services/Security/Security.API/Security.API.csproj
```

Development config varsayılan bağlantıları:

```text
PostgreSQL: localhost:5432
Redis: localhost:6379
```

Resend API key eksikse Infrastructure başlatılırken hata alınabilir. Lokal geliştirmede bu değer user secrets, environment variable veya appsettings üzerinden verilmelidir.

### 22.3 Test Çalıştırma

Tüm testler:

```bash
dotnet test
```

Sadece integration tests:

```bash
dotnet test tests/Security.IntegrationTests/Security.IntegrationTests.csproj
```

Integration tests Testcontainers kullandığı için Docker erişimi gerekir.

## 23. API Response Davranışı

Başarılı auth response'ları:

- Register: `201 Created`
- Login: `200 OK`
- Refresh: `200 OK`
- Logout/logout-all: `204 No Content`
- Forgot password: `202 Accepted`
- Reset password: `204 No Content`
- Verify email: `204 No Content`
- Resend verification: `202 Accepted`
- MFA setup begin/complete: `200 OK`
- MFA disable: `204 No Content`
- Session revoke: `204 No Content`

Hata response'ları genellikle ProblemDetails formatındadır:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/auth/register",
  "correlationId": "..."
}
```

Validation problem response'larında field bazlı error listesi bulunur.

## 24. Önemli Request/Response Contract'ları

Security.API contract dosyaları:

```text
src/Services/Security/Security.API/Contracts
```

Auth contract'ları:

- RegisterRequest / RegisterResponse
- LoginRequest / LoginResponse
- RefreshTokenRequest / RefreshTokenResponse
- ForgotPasswordRequest / ForgotPasswordResponse
- ResetPasswordRequest
- VerifyEmailRequest
- ResendVerificationRequest / ResendVerificationResponse
- BeginMfaSetupResponse
- CompleteMfaSetupRequest / CompleteMfaSetupResponse
- CompleteMfaLoginRequest
- DisableMfaRequest
- RegenerateRecoveryCodesRequest / RegenerateRecoveryCodesResponse
- CurrentUserResponse
- AuthTokensResponse
- UserResponse
- MfaChallengeResponse

Session contract:

- SessionResponse

Health contract'ları:

- HealthCheckResponse
- HealthCheckEntryResponse

## 25. Bağımlılık Özeti

### 25.1 Security.API

Önemli paketler:

- `MediatR`
- `Microsoft.AspNetCore.OpenApi`
- `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`
- `AspNetCore.HealthChecks.Redis`
- `Serilog.AspNetCore`
- `Serilog.Sinks.Console`
- `Scalar.AspNetCore`
- `OpenTelemetry.*`

### 25.2 Security.Application

Önemli paketler:

- `MediatR`
- `FluentValidation`
- `FluentValidation.DependencyInjectionExtensions`

### 25.3 Security.Infrastructure

Önemli paketler:

- `Microsoft.EntityFrameworkCore`
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.AspNetCore.Identity`
- `Microsoft.Extensions.Caching.StackExchangeRedis`
- `StackExchange.Redis`
- `System.IdentityModel.Tokens.Jwt`
- `Otp.NET`
- `Resend`
- `OpenIddict.*`

### 25.4 Gateway

Önemli paketler:

- `Yarp.ReverseProxy`
- `Microsoft.AspNetCore.Authentication.JwtBearer`

### 25.5 Transactions

Önemli paket:

- `Microsoft.AspNetCore.OpenApi`

## 26. Dikkat Edilmesi Gereken Noktalar

Bu bölüm mevcut kodun davranışını belgelemek için yazılmıştır; değişiklik önerisi olarak değil, proje okuma notu olarak değerlendirilmelidir.

1. `Security.Infrastructure.DependencyInjection` içinde `Resend:ApiKey` eksikse uygulama başlatma sırasında hata fırlatılır. Bu nedenle lokal ve container ortamlarında Resend ayarı sağlanmalıdır.
2. Gateway config içinde JWT için hem `SigningKey` hem `Key` bulunur; gateway kodu `Jwt:Key` okur, Security.Infrastructure ise `Jwt:SigningKey` okur.
3. Security.API içinde OpenAPI yalnızca Development ortamında map edilir.
4. Security.API Development ve IntegrationTests ortamlarında otomatik migration ve seed çalıştırır.
5. Transactions.WebAPI, Inputs API'nin ayakta olduğunu varsayar. Inputs API bu repo içinde yer almamaktadır.
6. Transactions `file-extract` endpoint'i extraction çağrısından sonra normalization çağrısı yapar ve son response olarak normalization sonucunu döndürür.
7. Transactions route'u gateway seviyesinde authenticated policy ile korunur; Transactions.WebAPI'nin kendi içinde JWT doğrulama yoktur.
8. Security.API access token revocation kontrollerini kendi JWT validation event'inde yapar. Gateway bu Redis revocation kontrollerini yapmaz.
9. MFA setup response içinde secret/manual key döner. Kullanıcı bu secret'ı authenticator uygulamasına eklemelidir.
10. Recovery code'lar plaintext olarak yalnızca üretildikleri anda response'ta döner; veritabanında hash saklanır.

## 27. Genel Akış Diyagramları

### 27.1 Normal Login

```text
Client
  -> POST /api/auth/login
  -> Security.API endpoint
  -> LoginCommand
  -> LoginCommandHandler
  -> UserRepository ile kullanıcı bulunur
  -> PasswordHasher ile şifre doğrulanır
  -> RefreshSession ve RefreshToken oluşturulur
  -> RoleRepository ile permission'lar okunur
  -> JwtTokenGenerator access token üretir
  -> AuditLog yazılır
  -> Access token + refresh token response döner
```

### 27.2 MFA Login

```text
Client
  -> POST /api/auth/login
  -> Şifre doğru
  -> Kullanıcıda MFA enabled
  -> RefreshSession ve RefreshToken oluşturulur
  -> MfaChallengeTokenService challenge token üretir
  -> requiresMfa=true response döner

Client
  -> POST /api/mfa/login/complete
  -> Challenge token validate edilir
  -> TOTP veya recovery code doğrulanır
  -> Access token üretilir
  -> Login response döner
```

### 27.3 Refresh Token Rotation

```text
Client
  -> POST /api/auth/refresh
  -> Refresh token hash'lenir
  -> Session ve token bulunur
  -> Eski token consumed yapılır
  -> Yeni access token üretilir
  -> Yeni refresh token üretilir
  -> Yeni refresh token hash'i session'a eklenir
  -> Yeni token çifti response döner
```

### 27.4 Logout

```text
Client
  -> POST /api/auth/logout
  -> Access token claim'lerinden user/session/jti okunur
  -> Session revoke edilir
  -> Refresh token'lar revoke edilir
  -> Redis'e jti revocation yazılır
  -> Redis'e session invalidation yazılır
  -> 204 No Content döner
```

## 28. Sonuç

Bu repo, finansal analiz uygulaması için güvenlik merkezli bir backend altyapısı sunar. En olgun ve detaylı bölüm Security servisidir. Security servisi register/login/refresh/logout, e-posta doğrulama, şifre sıfırlama, MFA, session yönetimi, permission tabanlı authorization, audit logging, rate limiting, Redis tabanlı access token invalidation ve PostgreSQL persistence özelliklerini kapsar.

API Gateway, Security ve Transactions servislerini tek dış giriş noktası arkasında toplar. Transactions servisi ise dış bir Inputs API'ye dosya, extraction, normalization ve AI işlemleri için geçiş katmanı sağlar.

Test tarafında özellikle Security.IntegrationTests projesi, gerçek PostgreSQL ve Redis container'larıyla kritik güvenlik akışlarını doğrulamak üzere yapılandırılmıştır. Bu da projenin güvenlik davranışlarının yalnızca unit seviyesinde değil, HTTP pipeline ve gerçek bağımlılıklara yakın ortamda da test edildiğini gösterir.
