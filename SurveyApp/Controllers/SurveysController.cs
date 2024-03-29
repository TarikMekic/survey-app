﻿using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SurveyApp.Database;
using SurveyApp.Database.Models;
using SurveyApp.Models.Questions;
using SurveyApp.Models.Surveys;

namespace SurveyApp.Controllers
{
    [Authorize]
    public class SurveysController : Controller
    {
        private readonly AppDbContext _db;

        public SurveysController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create([FromForm] CreateSurveyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }
            long userId = long.Parse(HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value);
            var survey = new Survey()
            {
                Name = model.Name,
                Description = model.Description,
                CreatedDate = DateTime.UtcNow,
                UserId = userId,
            };
            _db.Surveys.Add(survey);
            _db.SaveChanges();

            return Redirect("/Surveys/Index");
        }
        [HttpGet]
        public IActionResult Index()
        {
            long userId = long.Parse(HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier).Value);
            List<Survey> surveys = _db.Surveys.Where(survey => survey.UserId == userId).ToList();
            SurveyIndexViewModel surveyIndex = new SurveyIndexViewModel();
            surveyIndex.Surverys = new List<SurveyItemViewModel>();
            surveys.ForEach(survey =>
            {
                SurveyItemViewModel surveyItem = new SurveyItemViewModel();
                surveyItem.Id = survey.Id;
                surveyItem.Name = survey.Name;
                surveyItem.Description = survey.Description;
                surveyItem.CreatedDate = survey.CreatedDate;
                surveyIndex.Surverys.Add(surveyItem);
            });
            return View(surveyIndex);
        }
        [HttpGet]
        public IActionResult Result(long Id)
        {
            var survey = _db.Surveys.Where(survey => survey.Id == Id)
                .Include(survey => survey.Questions)
                .ThenInclude(Question => Question.Answers)
                .FirstOrDefault();
            SurveyResultViewModel surveyResult = new SurveyResultViewModel();
            List<ResultItemViewModel> rezultati = new List<ResultItemViewModel>();
            surveyResult.Name = survey.Name;
            surveyResult.Description = survey.Description;
            surveyResult.surveyId = survey.Id;
            survey.Questions.ForEach(question =>
            {
                List<AnswerItemViewModel> Answers = new List<AnswerItemViewModel>();
                question.Answers.ForEach(answer =>
                {
                    AnswerItemViewModel answerItemViewModel = new AnswerItemViewModel();
                    answerItemViewModel.AnswerId = answer.Id;
                    answerItemViewModel.Content = answer.Content;
                    Answers.Add(answerItemViewModel);
                });
                ResultItemViewModel resultItemViewModel = new ResultItemViewModel();
                resultItemViewModel.questionId = question.Id;
                resultItemViewModel.Content = question.Content;
                resultItemViewModel.Answers = Answers;
                rezultati.Add(resultItemViewModel);
            });
            surveyResult.Results = rezultati;
            return View(surveyResult);
        }

        [HttpGet]
        public IActionResult Edit(int? Id)
        {
            var survey = _db.Surveys.FirstOrDefault(x => x.Id == Id);
            EditSurveyViewModel model = new EditSurveyViewModel();
            model.Id = survey.Id;
            model.Name = survey.Name;
            model.Description = survey.Description;
            return View(model);

        }

        [HttpPost]
        public async Task<IActionResult> Edit([FromForm] EditSurveyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Edit");
            }

            var survey = _db.Surveys.FirstOrDefault(x => x.Id == model.Id);
            survey.Name = model.Name;
            survey.Description = model.Description;
            await _db.SaveChangesAsync();
            return Redirect("/Surveys/Index");
        }

        [HttpGet]
        public IActionResult Delete(long? Id)
        {
            var survey = _db.Surveys.FirstOrDefault(x => x.Id == Id);
            _db.Surveys.Remove(survey);
            _db.SaveChanges();
            return Redirect("/Surveys/Index");
        }

        [HttpGet]
        public IActionResult Details(long Id)
        {
            var survey = _db.Surveys.Include(x => x.Questions).FirstOrDefault(x => x.Id == Id);
            SurveyDetailsViewmodel model = new SurveyDetailsViewmodel();
            model.Id = Id;
            model.Name = survey.Name;
            model.Description = survey.Description;
            model.Questions = new List<QuestionItemViewModel>();
            survey.Questions.ForEach(q =>
            {
                QuestionItemViewModel question = new QuestionItemViewModel();
                question.Id = q.Id;
                question.Content = q.Content;
                question.SurveyId = q.SurveyId;
                model.Questions.Add(question);

            }
            );

            return View(model);
        }

        
        [HttpGet]
        [AllowAnonymous]
        public IActionResult TakeSurvey(long Id)
        {
            var survey =  _db.Surveys.Where(survey => survey.Id == Id)
                .Include(survey => survey.Questions).FirstOrDefault();
            List<TakeSurveyQuestionViewModel> questions = new List<TakeSurveyQuestionViewModel>();
            survey.Questions.ForEach(question =>
            {
                var questionViewModel = new TakeSurveyQuestionViewModel();
                questionViewModel.Question = question.Content;
                questionViewModel.QuestionId = question.Id; 
                questions.Add(questionViewModel); 
            });
            var takeSurveyModel = new TakeSurveyViewModel(); 
            takeSurveyModel.Name = survey.Name;
            takeSurveyModel.Description = survey.Description;
            takeSurveyModel.Questions = questions; 

            return View(takeSurveyModel); 

        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult TakeSurvey(TakeSurveyViewModel takeSurveyViewModel) 
        {
            if (!ModelState.IsValid)
            {
                return View("TakeSurvey", takeSurveyViewModel); 
            }
            
            takeSurveyViewModel.Questions.ForEach(question =>
            {
                var answer = new Answer()
                {
                    Content = question.Answer,
                    QuestionId = question.QuestionId,
                };

                _db.Answers.Add(answer); 
            });

            _db.SaveChanges();
            return View("ThankYou");
        }
    }
}