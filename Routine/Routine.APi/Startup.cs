using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using Routine.APi.Data;
using Routine.APi.Services;
using System;

namespace Routine.APi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // ע����� This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            /*
            * ����Э�̣�
            * ���һ����Ӧ�����ж��ֱ�����ʽ��ʱ��ѡȡ��ѵ�һ������������ application/json��application/xml
            * 
            * Accept Header ָ�������������ʽ����Ӧ ASP.NET Core ��� Output Formatters
            * �����������֧�ֿͻ��������ý�����ͣ�Media Type��������״̬��406
            * 
            * Content-Type Header ָ�������������ʽ����Ӧ ASP.NET Core ��� Input Formatters
            */

            /*
             * .Net Core Ĭ��ʹ�� Problem details for HTTP APIs RFC (7807) ��׼
             * - Ϊ���������Ϣ��Ӧ�ã�������ͨ�õĴ����ʽ
             * - ����ʶ������������ĸ� API
             */

            //������һ�ֽϾɵ�д�����ڱ���Ŀ�в�ʹ�ã���ƵP8��
            //services.AddControllers(options =>
            //{
            //    //����406״̬��
            //    options.ReturnHttpNotAcceptable = true;

            //    //OutputFormatters Ĭ������ֻ�� Json ��ʽ
            //    //��Ӷ���� XML ��ʽ��֧��
            //    //��ʱĬ�������ʽ��Ȼ�� Json ,��Ϊ Json ��ʽλ�ڵ�һλ��
            //    options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());

            //    //����� Index 0 λ�ò���� XML ��ʽ��֧�֣���ôĬ�������ʽ�� XML
            //    //options.OutputFormatters.Insert(0, new XmlDataContractSerializerOutputFormatter());
            //});
            //
            //�����ǽ��µ�д����AddXmlDataContractSerializerFormatters() �ȷ���ʹ�ø����㡣
            services.AddControllers(options =>
            {
                //����406״̬��
                options.ReturnHttpNotAcceptable = true;

            })
                //Ĭ�ϸ�ʽȡ�������л����ߵ����˳��
                .AddNewtonsoftJson(options =>  //������ JSON ���л��ͷ����л����ߣ����滻��ԭ��Ĭ�ϵ� JSON ���л����ߣ�����ƵP32��
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                })
                .AddXmlDataContractSerializerFormatters() //XML ���л��ͷ����л����ߣ���ƵP8��
                .ConfigureApiBehaviorOptions(options =>   //�Զ�����󱨸棨��ƵP29��
                {
                    //IsValid = false ʱ��ִ��
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var problemDetails = new ValidationProblemDetails(context.ModelState)
                        {
                            Type = "http://www.baidu.com",
                            Title = "���ִ���",
                            Status = StatusCodes.Status422UnprocessableEntity,
                            Detail = "�뿴��ϸ��Ϣ",
                            Instance = context.HttpContext.Request.Path
                        };
                        problemDetails.Extensions.Add("traceId", context.HttpContext.TraceIdentifier);
                        return new UnprocessableEntityObjectResult(problemDetails)
                        {
                            ContentTypes = { "application/problem+json" }
                        };
                    };
                });
            //ʹ�� AutoMapper��ɨ�赱ǰӦ��������� Assemblies Ѱ�� AutoMapper �������ļ�����ƵP12��
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            //AddScoped ���ÿһ�� HTTP ���󶼻Ὠ��һ���µ�ʵ��
            services.AddScoped<ICompanyRepository, CompanyRepository>();

            services.AddDbContext<RoutineDbContext>(options =>
            {
                options.UseSqlServer("Data Source=localhost;DataBase=routine;Integrated Security=SSPI");
            });
        }

        // ·���м�� This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            /*
             * ����м����˳��ǳ���Ҫ����������Ȩ�м��������Controller�ĺ�ߣ�
             * ��ô��ʹ��Ҫ��Ȩ����ô����Ҳ���ȵ���Controller��ִ������Ĵ��룬�����Ļ���Ȩ��û�������ˡ�
             */

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //500 ������Ϣ
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Unexpected Error!");
                    });
                });
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
