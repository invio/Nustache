﻿using System.Web.Mvc;

namespace Nustache.Mvc
{
    public class NustacheViewEngine : VirtualPathProviderViewEngine
    {
        public NustacheViewEngine(string fileExtension = "mustache")
        {
            MasterLocationFormats = new[]
                                        {
                                            "~/Views/Shared/{0}." + fileExtension
                                        };
            ViewLocationFormats = new[]
                                      {
                                          "~/Views/{1}/{0}." + fileExtension,
                                          "~/Views/Shared/{0}." + fileExtension
                                      };
            PartialViewLocationFormats = new[]
                                             {
                                                 "~/Views/{1}/{0}." + fileExtension,
                                                 "~/Views/Shared/{0}." + fileExtension
                                             };
            AreaViewLocationFormats = new[]
                                          {
                                              "~/Areas/{2}/Views/{1}/{0}." + fileExtension,
                                              "~/Areas/{2}/Views/Shared/{0}." + fileExtension
                                          };
            AreaPartialViewLocationFormats = new[]
                                                 {
                                                     "~/Areas/{2}/Views/{1}/{0}." + fileExtension,
                                                     "~/Areas/{2}/Views/Shared/{0}." + fileExtension
                                                 };
        }

        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
        {
            return new NustacheView(controllerContext, viewPath, masterPath);
        }

        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
        {
            return new NustacheView(controllerContext, partialPath, null);
        }
    }
}