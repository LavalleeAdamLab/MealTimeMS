using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Data;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using MealTimeMS.Util;

using Accord.Statistics.Models.Regression.Fitting;
using Accord.Statistics.Models.Regression;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using Accord.IO;
using MealTimeMS.IO;


namespace MealTimeMS.ExclusionProfiles.MachineLearningGuided
{
	//This class is used to train the LR model, still havn't decided which C# ML library to stick with, debating between Accord.Net and ML.Net

	public class IdentificationLogisticRegressionTrainer
	{
		static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
		static double minimumSpecificity = 0.95;
		static float tick = 0.001F;

		public static String _modelPath = InputFileOrganizer.OutputFolderOfTheRun +"\\"+ "model.zip";
		static MLContext mlContext = new MLContext();
		static StreamWriter sw;
		static String SVMSaveFile;
		public static String TraingAndWriteAccordModel(String trainingFile, String savedWeightDirectory)
		{
			String TrainingResultOutputFile = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun,"ClassifierTrainingLog.txt");
			StreamWriter trainingLog = new StreamWriter(TrainingResultOutputFile);

			log.Info("Training Accord.Net logistic regression classifier using feature set {0}",trainingFile);
			trainingLog.WriteLine("Training Accord.Net logistic regression classifier using feature set {0}", trainingFile);
			String trainingFileBaseName = Path.GetFileNameWithoutExtension(trainingFile);
			String savedCoefficient = Path.Combine(savedWeightDirectory, trainingFileBaseName + ".ClassifierCoefficient.txt");
			IDataView trainingSet = LoadData(mlContext, trainingFile);
			var lrAccord = TrainAccordModel(trainingSet);
			StreamWriter sw2 = new StreamWriter(savedCoefficient);
			Console.WriteLine("Accord weights");
			foreach (double w in lrAccord.Weights)
			{
				Console.WriteLine(w);
				sw2.Write(w + "\t");
			}
			Console.WriteLine("intercept: {0}", lrAccord.Intercept);
			sw2.WriteLine();
			sw2.WriteLine(lrAccord.Intercept);
			sw2.Close();
			String savedFile = Path.Combine(savedWeightDirectory, "accordSerializerSavedFile.txt");
			Serializer.Save(obj: lrAccord, path: savedFile);

			
			List<ROCDataPoint> AccordROC = ROCCurve(lrAccord, trainingSet);
			double lrAUC = GetAUCFromROC(AccordROC);
			double settedThreshold = ThresholdFromROC(AccordROC, minimumSpecificity: 0.95);
			trainingLog.WriteLine("Area-under-the-curve: {0}", lrAUC);
			trainingLog.WriteLine("Classifier threshold at 0.95 specificity: {0}", settedThreshold);
			trainingLog.WriteLine("Classifier Coefficient file saved at:\n{0}\nInclude this coefficient file in the parameters to run MealTimeMS", savedFile);
			trainingLog.Close();
			return savedCoefficient;
		}

		public static void TestLRModel(String lrSavedCoeff,String trainingFile, String testingFile)
		{
			var lr = Loader.LoadAccordNetLogisticRegressionModel(lrSavedCoeff);

			IDataView trainingSet = LoadData(mlContext, trainingFile);
			IDataView testingSet = LoadData(mlContext, testingFile);
			double[] correctLabels = IDataViewToAccord(testingSet).labels;
	
			List<ROCDataPoint> AccordROC = ROCCurve(lr, trainingSet);
			double settedThreshold = ThresholdFromROC(AccordROC, minimumSpecificity: 0.95);
			double lrAUC = GetAUCFromROC(AccordROC);
			bool[] lrOutput = AccordDecide( lr, testingSet, settedThreshold);
			MLPerformance accordEvaluation = EvaluateResults(correctLabels, lrOutput);

			Console.WriteLine("AUC:{0}\tSpecificity:{1}\tAccuracy:{2}\tThreshold:{3}", lrAUC, accordEvaluation.getSpecificity(), accordEvaluation.getAccuracy(), settedThreshold);
			

		}

		public static void DoJob()
		{

			//String testingFile = InputFileOrganizer.DataRoot + "DCN_TestingSet120_NoDecoy.csv";
			//String trainingFile = InputFileOrganizer.DataRoot + "240minTestingSetDCN_NoDecoy.csv";
			String testingFile = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\TestData\\240minTestingSetDCN_NoDecoy.csv";
			String trainingFile = "C:\\Coding\\2019LavalleeLab\\GitProjectRealTimeMS\\TestData\\240minTestingSetDCN_NoDecoy.csv";


			IDataView trainingSet = LoadData(mlContext, trainingFile);
			IDataView testingSet = LoadData(mlContext, testingFile);
			sw = new StreamWriter(Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, "LogisticRegressionTrainerOutput.txt"));
			StreamWriter sw2 = new StreamWriter(Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, "AccordWeight.txt"));

			double[] correctLabels = IDataViewToAccord(testingSet).labels;


			var lrAccord = TrainAccordModel(trainingSet);

			
			Console.WriteLine("Accord weights");
			foreach(double w in lrAccord.Weights)
			{
				Console.WriteLine(w);
				sw2.Write(w + "\t");
			}
			Console.WriteLine("intercept: {0}", lrAccord.Intercept);
			sw2.WriteLine();
			sw2.WriteLine(lrAccord.Intercept);
			sw2.Close();

			
			Console.WriteLine("--------------------");
			//LogisticRegression lrAccord = new LogisticRegression();
			////spark weights
			//lrAccord.Weights = new double[] { -0.014101223713988448, 0.40498899120575244, -0.4050931006103277, -0.6514251562095439, -1.4199639211914807, -0.00154170434120518, -0.0017589165180070616, -0.001427050540781882, -0.006890591731651152, 0.23434955458842885, 0.24386505335051745, 0.25265687551174654, 0.34976191542247076, 0.17989186249395828, 0.15598728100439885 };
			//lrAccord.Intercept = -2.0771355924182346;
			List<ROCDataPoint> AccordROC= ROCCurve(lrAccord, trainingSet);
			double lrAUC = GetAUCFromROC(AccordROC);
			Console.WriteLine("AUC= " + lrAUC);
			//ShowAccordIntermediateData(trainingSet, lrAccord);
			double settedThreshold = ThresholdFromROC(AccordROC, minimumSpecificity: 0.95);
			Console.WriteLine("Accord threshold: " + settedThreshold);
			bool[] lrOutput = AccordDecide(lrAccord, testingSet,settedThreshold);
			//bool[] lrOutput = AccordDecide(lrAccord, testingSet,0.3);
		
			//var kk = EvaluateResults(IDataViewToAccord(trainingSet).labels, AccordDecide(lrAccord, trainingSet, settedThreshold));
			//double AccordTrainingSpecificity = kk.getSpecificity();
			MLPerformance accordEvaluation = EvaluateResults(correctLabels, lrOutput);





			//BuildMLModel(0.95, trainingSet,testingSet);

			if (false)
			{

			var MLNetModel = BuildAndTrainModel(trainingSet);
			//ViewIntermediateData(model.Transform(testingSet));
			IDataView predictions = MLNetModel.Transform(testingSet);
			bool[] mlOutput = ExtractSVMPrediction(predictions);
			//CalibratedBinaryClassificationMetrics MLMetrics = GetMetrics(MLNetModel, testingSet);
			MLPerformance mlEvaluation = EvaluateResults(correctLabels, mlOutput);
			}




			SupportVectorMachine<Gaussian> svm;
			//SVMSaveFile = Path.Combine(InputFileOrganizer.DataRoot, "SVMParams_trainedOn240DCN.txt");
			SVMSaveFile = "";
			if (SVMSaveFile.Equals(""))
			{
				SVMSaveFile = Path.Combine(InputFileOrganizer.OutputFolderOfTheRun, "SVMParams.txt");
				svm = TrainSVMModel(trainingSet);
				Serializer.Save(obj: svm, path: SVMSaveFile);
			}
			else
			{
				svm = Serializer.Load(SVMSaveFile, out svm);
			}
			bool[] svmOutput = SVMDecide(svm,testingSet);
			MLPerformance svmEvaluation = EvaluateResults(correctLabels, svmOutput);

			Output("Training Set: {0}\nTesting Set: {1}", trainingFile, testingFile);
			Output("Accord\t ML.Net \tSVM:");
			//Console.WriteLine("AUC: {0}\t {1}", lrAUC, MLMetrics.AreaUnderRocCurve);
			Output("AUC: {0}\t {1}\t {2}", lrAUC, 0, 0);

			//Console.WriteLine("Specificity: {0}\t {1}\t {2}", AccordTrainingSpecificity, mlEvaluation.getSpecificity(), -1);
			//Console.WriteLine("Accuracy: {0}\t {1}\t {2}", accordEvaluation.getAccuracy(), mlEvaluation.getAccuracy(),-1);

			Output("Specificity: {0}\t {1}\t {2}", accordEvaluation.getSpecificity(),0, /* mlEvaluation.getSpecificity(),*/ svmEvaluation.getSpecificity());
			Output("Accuracy: {0}\t {1}\t {2}", accordEvaluation.getAccuracy(),0,  /*mlEvaluation.getAccuracy(), */ svmEvaluation.getAccuracy());
			//model = SetThreshold( model, testingSet);



			
			//UseModelWithSingleItem(mlContext, model);
			sw.Close();
		}
		private static void BuildMLModel(double minimumSpecificity, IDataView trainingSet, IDataView testingSet)
		{
			ITransformer model = BuildAndTrainModel(trainingSet);
			//ITransformer model = BuildAndTrainSVMModel(trainingSet);
			IDataView trainingSummary = model.Transform(trainingSet);
			IDataView predictions = model.Transform(testingSet);
			List<ROCDataPoint> rocCurve = ROCCurve(trainingSummary);
			double auc = GetAUCFromROC(rocCurve);
			double threshold = ThresholdFromROC(rocCurve, minimumSpecificity);
			
			var trainingPerformance = EvaluateResults(trainingSummary, threshold);
			var performance = EvaluateResults(predictions, threshold);

			Console.WriteLine("AUC: {0}\t Specificity: {1}\t Accuracy: {2}",auc,trainingPerformance.getSpecificity(), performance.getAccuracy());
			Console.WriteLine("ML threshold: {0}", threshold);
			mlContext.Model.Save(model, trainingSet.Schema, _modelPath);

		}
		

		public static bool[] AccordDecide(LogisticRegression lrModel, IDataView input, double threshold)
		{
			var convertedInput = IDataViewToAccord(input).inputs;
			var lrProbability = lrModel.Probabilities(convertedInput);
			bool[] output = new bool[lrProbability.Length];
			for(int i = 0; i < lrProbability.Length; i++)
			{
				output[i] = lrProbability[i][1]>=threshold;
			}
			return output;

		}

		public static bool[] SVMDecide(SupportVectorMachine<Gaussian> svm , IDataView input)
		{
			
			var convertedInput = IDataViewToAccord(input).inputs;
			//var lrScore = svm.Score(convertedInput);
			//var lrProbability = svm.Probabilities(convertedInput);
			//bool[] output = new bool[lrProbability.Length];
			//for (int i = 0; i < lrProbability.Length; i++)
			//{
			//	output[i] = lrProbability[i][1] >= threshold;
			//}
			return svm.Decide(convertedInput);

		}



		private static List<ROCDataPoint> ROCCurve(LogisticRegression accordModel, IDataView data)
		{
			var convertedInput = IDataViewToAccord(data).inputs;
			var lrProbability = accordModel.Probabilities(convertedInput);
			double[] probabilities = new double[lrProbability.Length];
			for (int i = 0; i < probabilities.Length; i++)
			{
				probabilities[i] = lrProbability[i][1];
			}

			double[] correctLabels = IDataViewToAccord(data).labels;
			return ROCCurve(correctLabels, probabilities);

		}
		private static bool[] ExtractPrediction(IDataView data)
		{
			var predictList = mlContext.Data.CreateEnumerable<ProteinPrediction>(data, true);
			bool[] prediction = new bool[predictList.Count()];
	
			int count = 0;
			foreach (ProteinPrediction p in predictList)
			{

				prediction[count] = p.Prediction;
				count++;
			}
			return prediction;
		}
		private static bool[] ExtractSVMPrediction(IDataView data)
		{
			var predictList = mlContext.Data.CreateEnumerable<SVMProteinPrediction>(data, true);
			bool[] prediction = new bool[predictList.Count()];

			int count = 0;
			foreach (SVMProteinPrediction p in predictList)
			{

				prediction[count] = p.Prediction;
				count++;
			}
			return prediction;
		}
		private static double[] ExtractProbabilities(IDataView data)
		{
			var predictList = mlContext.Data.CreateEnumerable<ProteinPrediction>(data, true);
			double[] probabilities = new double[predictList.Count()];
			int count = 0;
			foreach (ProteinPrediction p in predictList)
			{
				probabilities[count] = (double)p.Probability;
				count++;
			}
			return probabilities;
		}
		private static double[] ExtractLabels(IDataView data)
		{
			var predictList = mlContext.Data.CreateEnumerable<ProteinPrediction>(data, true);
			double[] correctLabels = new double[predictList.Count()];
			int count = 0;
			foreach (ProteinPrediction p in predictList)
			{
				correctLabels[count] = p.Label ? 1 : 0;
				count++;
			}
			return correctLabels;
		}

		private static List<ROCDataPoint> ROCCurve( IDataView MLPredictions)
		{
			double[] probabilities = ExtractProbabilities(MLPredictions);
			double[] correctLabels = ExtractLabels(MLPredictions);

			return ROCCurve(correctLabels, probabilities);

		}
		private static List<ROCDataPoint> ROCCurve(double[] correctLabels, double[] probabilities)
		{
			
			double tempThreshold = 1.0;
			double tick = 0.001;
			List<ROCDataPoint> rocDataPoints= new List<ROCDataPoint>();
		
			//sw.WriteLine("ROC data points:");
			do
			{
				var dot = EvaluateResults(correctLabels, probabilities, tempThreshold);
				rocDataPoints.Add(new ROCDataPoint { tpr = dot.getTPR(), fpr = dot.getFPR(), threshold = tempThreshold , mlp = dot});
				//sw.WriteLine(dot.getFPR() + "\t" + dot.getTPR());

				tempThreshold -= tick;
			} while (tempThreshold >= 0);
			//sw.Flush();
			return rocDataPoints;
		}

		private static double ThresholdFromROC(List<ROCDataPoint> rocDataPoints, double minimumSpecificity)
		{
			double maxFPR = 1 - minimumSpecificity;
			foreach (ROCDataPoint p in rocDataPoints)
			{
				if (p.fpr >= maxFPR)
				{
					return p.threshold;
				}
			}
			return -1;
		}

		private static Double GetAUCFromROC(List<ROCDataPoint> rocDataPoints)
		{
			double auc = 0;
			for (int i = 0; i < rocDataPoints.Count - 1; i++)
			{
				auc += (rocDataPoints[i + 1].tpr + rocDataPoints[i].tpr) / 2 * (rocDataPoints[i + 1].fpr - rocDataPoints[i].fpr);
			}
			auc += 1 - rocDataPoints[rocDataPoints.Count - 1].fpr;

			return auc;
		}

		private static void ShowAccordIntermediateData(IDataView input, LogisticRegression accordModel)
		{
			var convertedInput = IDataViewToAccord(input).inputs;
			bool[] lrOutput = accordModel.Decide(convertedInput);
			var lrScore = accordModel.Scores(convertedInput);
			var lrProbability = accordModel.Probabilities(convertedInput);
			var inputEnumerable = mlContext.Data.CreateEnumerable<IDataViewWrapper>(input, true);
			sw.WriteLine("Label\tScore\tProbability\tPrediction");
			int count = 0;
			foreach (IDataViewWrapper pp in inputEnumerable)
			{
				int label = pp.Label ? 1 : 0;
				int prediction = lrOutput[count] ? 1 : 0;
				double score = lrScore[count][1];
				double probability = lrProbability[count][1];
				sw.WriteLine("{0}\t{1}\t{2}\t{3}", label, score, probability, prediction);
				count++;
			}
			sw.Flush();

		}
		private static void ShowMLIntermediateData(IDataView Result)
		{
			var predictionsEnumerator = mlContext.Data.CreateEnumerable<ProteinPrediction>(Result, true);
			sw.WriteLine("Label\tScore\tProbability\tPrediction");
			foreach (ProteinPrediction pp in predictionsEnumerator)
			{
				int label = pp.Label ? 1 : 0;
				int prediction = pp.Prediction ? 1 : 0;
				sw.WriteLine("{0}\t{1}\t{2}\t{3}", label, pp.Score, pp.Probability, prediction);
			}

		}
		private static MLPerformance EvaluateResults(double[] label, bool[] predictions)
		{
			MLPerformance mlp = new MLPerformance();
			for (int i = 0; i < label.Length; i++)
			{
				JudgePerformance(ref mlp, (int)label[i] == 1, predictions[i]);
			}
			return mlp;
		}
		private static MLPerformance EvaluateResults(double[] label,  double[] probabilities, double threshold)
		{
			MLPerformance mlp = new MLPerformance();
			for (int i = 0; i < label.Length; i++)
			{
				JudgePerformance(ref mlp, (int)label[i] == 1, probabilities[i] >= threshold);
			}
			return mlp;
		}

		private static MLPerformance EvaluateResults(IDataView predictions, double threshold)
		{
			double[] probabilities = ExtractProbabilities(predictions);
			double[] labels = ExtractLabels(predictions);
			return EvaluateResults(labels, probabilities, threshold);
		}
		private static void JudgePerformance(ref MLPerformance mlp, bool label, bool prediction)
		{
			if (label)
			{
				if (prediction)
				{
					mlp.tp++;
				}
				else
				{
					mlp.fn++;
				}
			}
			else
			{
				if (prediction)
				{
					mlp.fp++;
				}
				else
				{
					mlp.tn++;
				}
			}
		}
		private static SupportVectorMachine<Gaussian> TrainSVMModel(IDataView trainingData)
		{
			AccordIO data = IDataViewToAccord(trainingData);
			Console.WriteLine("Creating and training Poly kernel SVM");
			var smo = new SequentialMinimalOptimization<Gaussian>();
			smo.UseKernelEstimation = true;
			smo.UseComplexityHeuristic = true;
			smo.Epsilon = 1.0e-3;
			smo.Tolerance = 1.0e-2;

			Console.WriteLine("Starting training");
			var svm = smo.Learn(data.inputs, data.labels);
			Console.WriteLine("Training complete");
			//double[][] sVectors = svm.SupportVectors;
			//for (int i = 0; i < sVectors.Length; ++i)
			//{
			//	for (int j = 0; j < sVectors[i].Length; ++j)
			//	{
			//		svmExporter.Write(sVectors[i][j].ToString("F1") + " ");
			//	}
			//	svmExporter.WriteLine("");
			//}

			//}AccordIO data = IDataViewToAccord(trainingData);
			//Console.WriteLine("Creating and training Poly kernel SVM");
			//var smo = new SequentialMinimalOptimization<Polynomial>();
			////smo.UseKernelEstimation = true;
			//smo.UseComplexityHeuristic = true;
			//smo.Kernel = new Polynomial(20, 0.0);
			
			////smo.Complexity = 2.0;
			//smo.Epsilon = 1.0e-3;
			//smo.Tolerance = 1.0e-2;

			//Console.WriteLine("Starting training");
			//var svm = smo.Learn(data.inputs, data.labels);
			//Console.WriteLine("Training complete");

			//Console.WriteLine("Model support vectors: ");
			//double[][] sVectors = svm.SupportVectors;
			//for (int i = 0; i < sVectors.Length; ++i)
			//{
			//	for (int j = 0; j < sVectors[i].Length; ++j)
			//	{
			//		Console.Write(sVectors[i][j].ToString("F1") + " ");
			//	}
			//	Console.WriteLine("");
			//}

			return svm;

		}

		private static LogisticRegression TrainAccordModel(IDataView trainingData)
		{

			AccordIO data = IDataViewToAccord(trainingData);

			var trainer = new IterativeReweightedLeastSquares<LogisticRegression>()
			{
				MaxIterations = 1000,
				Regularization = 1e-6
			};

			// Use the teacher algorithm to learn the regression:
			LogisticRegression lr = trainer.Learn(data.inputs, data.labels);

			return lr;
			// Classify the samples using the model
			//bool[] answers = lr.Decide(inputs);

		}
		

		private static AccordIO IDataViewToAccord(IDataView data)
		{
			var list = mlContext.Data.CreateEnumerable<IDataViewWrapper>(data, reuseRowObject: true);
			double[][] inputs = new double[list.Count()][];
			double[] labels = new double[list.Count()];
			int count = 0;

			foreach (IDataViewWrapper pp in list)
			{
				float[] features = pp.Features;
				inputs[count] = new double[features.Length];
				for (int i = 0; i < features.Length; i++)
				{
					inputs[count][i] = (double)features[i];
				}
				labels[count] = pp.Label ? 1 : 0;

				count++;
			}
			return new AccordIO(inputs, labels);
		}


		private static void ViewIntermediateData(IDataView data)
		{
			IEnumerable<ProteinPrediction> dataList = mlContext.Data.
			   CreateEnumerable<ProteinPrediction>(data, reuseRowObject: true);

			int pos = 0;
			int neg = 0;

			foreach (ProteinPrediction protein in dataList)
			{
				//Console.WriteLine("Accession: {0}, Features: \n", protein.Accession);
				//foreach(float f in   protein.Features){
				//    Console.Write(f + " ");
				//}
				Console.WriteLine("Prediction: {0:0.00}, Probability: {1:0.00}, Score: {2:0.00}", protein.Prediction, protein.Probability, protein.Score);
				if (protein.Prediction)
				{
					pos++;
				}
				else
				{
					neg++;
				}
			}
			Console.WriteLine("Pos: {0}, Neg: {1}", pos, neg);
		}
		public static BinaryPredictionTransformer<Microsoft.ML.Calibrators.CalibratedModelParametersBase<LinearBinaryModelParameters, Microsoft.ML.Calibrators.PlattCalibrator>>
			BuildAndTrainModel(IDataView trainingSet)
		{

			//IEstimator<ITransformer> dataPrepEstimator =
			//mlContext.Transforms.Concatenate("Features", "Cardinality", "HighestConfidenceScore");

			//// Create data prep transformer
			//ITransformer dataPrepTransformer = dataPrepEstimator.Fit(splitTrainSet);

			//// Apply transforms to training data
			//IDataView transformedTrainingData = dataPrepTransformer.Transform(splitTrainSet);
			// var estimator = mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(IDataViewWrapper.Features));
			var trainer = (mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));

			Console.WriteLine("=============== Create and Train the Model ===============");

			BinaryPredictionTransformer<Microsoft.ML.Calibrators.CalibratedModelParametersBase<LinearBinaryModelParameters, Microsoft.ML.Calibrators.PlattCalibrator>> 
				model = trainer.Fit(trainingSet);

			var weights = model.Model.SubModel.Weights;
			var bias = model.Model.SubModel.Bias;
			Output("ML.Net Weights: ");
			foreach (float w in weights)
			{
				Output(w.ToString());
			}
			Output(bias.ToString());

			Console.WriteLine("=============== End of training ===============");
			Console.WriteLine();
			return model;
		}
		public static ITransformer BuildAndTrainSVMModel(IDataView trainingSet)
		{

			//IEstimator<ITransformer> dataPrepEstimator =
			//mlContext.Transforms.Concatenate("Features", "Cardinality", "HighestConfidenceScore");

			//// Create data prep transformer
			//ITransformer dataPrepTransformer = dataPrepEstimator.Fit(splitTrainSet);

			//// Apply transforms to training data
			//IDataView transformedTrainingData = dataPrepTransformer.Transform(splitTrainSet);
			// var estimator = mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(IDataViewWrapper.Features));
			var trainer = (mlContext.BinaryClassification.Trainers.LinearSvm(labelColumnName: "Label", featureColumnName: "Features", null, numberOfIterations: 50));

			Console.WriteLine("=============== Create and Train the Model ===============");

			var model = trainer.Fit(trainingSet);

			
			
			Console.WriteLine("=============== End of training ===============");
			Console.WriteLine();
			return model;
		}
		public static ITransformer BuildAndTrainModelCrossValidate(IDataView trainingSet)
		{
			var trainer = (mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features"));

			Console.WriteLine("=============== Create and Train the Model ===============");
			var cvResults = mlContext.BinaryClassification.CrossValidate(trainingSet, trainer, numberOfFolds: 10);
			double[] specificity = cvResults.OrderByDescending(fold => fold.Metrics.NegativeRecall).Select(fold => fold.Metrics.NegativeRecall).ToArray();
			double[] AUC = cvResults.OrderByDescending(fold => fold.Metrics.NegativeRecall).Select(fold => fold.Metrics.AreaUnderRocCurve).ToArray();
			for (int i = 0; i < AUC.Length; i++)
			{

				Console.WriteLine("Specificity: {0:0.00}, AUC: {1:0.00}", specificity[i], AUC[i]);
			}
			ITransformer[] models = cvResults.OrderByDescending(fold => fold.Metrics.NegativeRecall).Select(fold => fold.Model).ToArray();

			Console.WriteLine("=============== End of training ===============");
			Console.WriteLine();
			return models[models.Length - 1];
		}

		private static BinaryPredictionTransformer<Microsoft.ML.Calibrators.CalibratedModelParametersBase<LinearBinaryModelParameters, Microsoft.ML.Calibrators.PlattCalibrator>>
			SetThreshold(BinaryPredictionTransformer<Microsoft.ML.Calibrators.CalibratedModelParametersBase<LinearBinaryModelParameters, Microsoft.ML.Calibrators.PlattCalibrator>> lrModel, IDataView testSet)
		{

			float threshold = 1.0F;
			double currentSpecificity = 1.0;
			do
			{
				threshold -= tick;

				CalibratedBinaryClassificationMetrics metrics = GetMetrics((ITransformer)lrModel, testSet);
				currentSpecificity = metrics.NegativeRecall;

				double AUC = metrics.AreaUnderRocCurve;

				Console.WriteLine("Threshold: {0:0.00}; Specificity: {1:0.00}; AUC: {2:0.00}", threshold, currentSpecificity, AUC);


				lrModel = mlContext.BinaryClassification.ChangeModelThreshold(lrModel, threshold);

				Thread.Sleep(5);
			} while (currentSpecificity > minimumSpecificity);
			return lrModel;

		}

		private static CalibratedBinaryClassificationMetrics GetMetrics(ITransformer model, IDataView testSet)
		{
			IDataView predictions = model.Transform(testSet);
			return mlContext.BinaryClassification.Evaluate(predictions);
		}

		public static IDataView LoadData(MLContext mlContext, String path)
		{

			DataTable dt = IdentificationFeatureExtractionUtil.loadDataTable(path);
			IDataView dataView = IdentificationFeatureExtractionUtil.transformFeatures(dt, true);
			
			//TrainTestData splitDataView = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
			return dataView;

		}
		private static void Output(String str)
		{
			sw.WriteLine(str);
			Console.WriteLine(str);
			sw.Flush();
		}

		private static void Output(String mainStr, params object[] input)
		{
			
			String[] substitutes = new String[ input.Length];
			for (int i =0; i<input.Length; i++)
			{
				substitutes[i] = input[i].ToString();
			}
			String strToOutput= String.Format(mainStr,substitutes);
			Output(strToOutput);
		}

		public static void Evaluate(ITransformer model, IDataView testSet)
		{

			Console.WriteLine("=============== Evaluating Model accuracy with Test data===============");
			CalibratedBinaryClassificationMetrics metrics = GetMetrics(model, testSet);


			Console.WriteLine("Sensitivity: {0}", metrics.PositiveRecall);
			Console.WriteLine("Specificity: {0}", metrics.NegativeRecall);
			Console.WriteLine();
			Console.WriteLine("Model quality metrics evaluation");
			Console.WriteLine("--------------------------------");
			Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
			Console.WriteLine($"Auc: {metrics.AreaUnderRocCurve:P2}");
			Console.WriteLine($"F1Score: {metrics.F1Score:P2}");
			Output("AUC" + metrics.AreaUnderRocCurve);
			Output("Accuracy" + metrics.Accuracy);
			Output("Specificity" + metrics.NegativeRecall);
			Console.WriteLine("=============== End of model evaluation ===============");
		}
		private static void UseModelWithSingleItem(MLContext mlContext, ITransformer model)
		{
			//PredictionEngine<IDataViewWrapper, ProteinPrediction> predictionFunction = mlContext.Model.CreatePredictionEngine<IDataViewWrapper, ProteinPrediction>(model);
			//IDataViewWrapper sampleStatement = new IDataViewWrapper
			//{

			//};
		}

	}
	public class AccordIO
	{
		public double[][] inputs;
		public double[] labels;
		public AccordIO(double[][] _inputs, double[] _labels)
		{
			inputs = _inputs;
			labels= _labels;
		}
	}

	public class MLPerformance
	{
		public int tp = 0;
		public int fp = 0;
		public int tn = 0;
		public int fn = 0;

		public double getAccuracy()
		{
			return (double)(tp + tn) / (double)getTotalCase();
		}

		public double getSpecificity()
		{
			return 1 - getFPR();
		}
		public double getFPR()
		{
			return (double)(fp) / (double)(fp + tn);
		}
		public double getTPR()
		{
			return (double)(tp) / (double)(tp + fn);
		}

		public int getTotalCase()
		{
			return tp + fp + tn + fn;
		}
	}

	public class ROCDataPoint{
		public double tpr;
		public double fpr;
		public double threshold;
		public MLPerformance mlp;
		public override string ToString()
		{
			return String.Format("TPR: {0:0.000}| FPR: {1:0.000}| Threshold: {2:0.000}",tpr,fpr, threshold);
		}

	}


}
