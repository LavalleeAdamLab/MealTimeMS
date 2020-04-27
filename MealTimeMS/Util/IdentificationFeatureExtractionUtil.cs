using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MealTimeMS.Data.Graph;
using MealTimeMS.ExclusionProfiles.MachineLearningGuided;

using Microsoft.ML;
using Microsoft.ML.Data;
using System.Data;
using Accord.Statistics.Models.Regression;
using Accord.MachineLearning.VectorMachines;
using Accord.Statistics.Kernels;
using Accord.IO;

namespace MealTimeMS.Util
{


	public class IdentificationFeatureExtractionUtil
	{

#if STDEVINCLUDED
		private readonly static String[] FEATURE_LIST = new String[] { "Cardinality",
					"HighestConfidenceScore", "MeanConfidenceScore", "MedianConfidenceScore","HighestDCN", "MeanDCN","MedianDCN", "StDevConfidenceScore" };
		private readonly static String[] TABLE_HEADERS = new String[] { "Accession", "Cardinality",
					"HighestConfidenceScore", "MeanConfidenceScore", "MedianConfidenceScore","HighestDCN", "MeanDCN","MedianDCN", "StDevConfidenceScore" };
#else
		private readonly static String[] FEATURE_LIST = new String[] { "Cardinality",
					"HighestConfidenceScore", "MeanConfidenceScore", "MedianConfidenceScore", "HighestDCN", "MeanDCN","MedianDCN"};
		private readonly static String[] TABLE_HEADERS = new String[] { "Accession", "Cardinality",
					"HighestConfidenceScore", "MeanConfidenceScore", "MedianConfidenceScore","HighestDCN", "MeanDCN","MedianDCN"};
#endif


		private readonly static Type[] TABLE_DATATYPE = {  typeof(String), typeof(int),
					  typeof(double), typeof(double), typeof(double), typeof(double),  typeof(double), typeof(double), typeof(double)};

		private readonly static double DEFAULT_STDEV_MAX = 0.6123073103; // TODO what is an appropriate number

		private static DataTable DataTableSchema = CreateEmptyTable();

		private static DataTable CreateEmptyTable()
		{
			DataTable featureTable = new DataTable("featureTable");

			DataColumn colAccession = new DataColumn(TABLE_HEADERS[0], TABLE_DATATYPE[0]);
			featureTable.Columns.Add(colAccession);
			for (int i = 1; i < TABLE_HEADERS.Length; i++)
			{
				DataColumn colDate = new DataColumn(TABLE_HEADERS[i], TABLE_DATATYPE[i]);
				featureTable.Columns.Add(colDate);
			}

			// Set the OrderId column as the primary key.
			featureTable.PrimaryKey = new DataColumn[] { colAccession };

			return featureTable;
		}
		

		public static IdentificationFeatures extractFeatures(String accession, List<PeptideScore> peptideScores)
		{


			int cardinality = peptideScores.Count;
			if (cardinality < 1)
			{
				return new IdentificationFeatures(accession, cardinality, 0, 0, 0, 0, 0, 0 ,0);
			}


			//TODO should this be number of unique peptides or number of peptide scores??
			Double highestConfidenceScore = Double.MinValue;
			Double meanConfidenceScore;
			Double medianConfidenceScore;
			Double stdevConfidenceScore;

			Double highestDCN = Double.MinValue;
			Double meanDCN;
			Double medianDCN;


			double[] scores = new double[cardinality];
			double[] dCNList = new double[cardinality];
			for (int i = 0; i < peptideScores.Count; i++)
			{
				PeptideScore s = peptideScores[i];
				Double confidenceScore = s.getXCorr();
				Double dCN = s.getdCN();
				scores[i] = confidenceScore;
				dCNList[i] = dCN;
				// calculate highest confidence score
				if (confidenceScore > highestConfidenceScore)
				{
					highestConfidenceScore = confidenceScore;
				}
				if(dCN > highestDCN)
				{
					highestDCN = dCN;
				}
			}

	
			// calculate mean
			meanConfidenceScore = scores.Average();
			meanDCN = dCNList.Average();

			// calculate median
			medianConfidenceScore = CalculateMedian(scores);
			medianDCN = CalculateMedian(dCNList);
			// calculate stdev
			// bias correction set to true
			// that means the stdev formula uses "N-1" as the denominator, I believe.
			// this helps to estimate the variance more accurately for a small N

			stdevConfidenceScore = CalculateStdDev(scores);

			IdentificationFeatures f = new IdentificationFeatures(accession, cardinality, highestConfidenceScore, meanConfidenceScore, medianConfidenceScore,
				highestDCN, meanDCN, medianDCN, stdevConfidenceScore);
			return f;
		}

		private static double CalculateMedian(double[] values)
		{
			double[] tempArray = values;
			int count = tempArray.Length;

			Array.Sort(tempArray);

			double medianValue = 0;

			if (count % 2 == 0)
			{
				// count is even, need to get the middle two elements, add them together, then divide by 2
				double middleElement1 = tempArray[(count / 2) - 1];
				double middleElement2 = tempArray[(count / 2)];
				medianValue = (middleElement1 + middleElement2) / 2;
			}
			else
			{
				// count is odd, simply get the middle element.
				medianValue = tempArray[(count / 2)];
			}
			return medianValue;
		}

		private static double CalculateStdDev(double[] values)
		{
			double stdevOut = 0;
			if (values.Count() > 1)
			{
				//Compute the Average      
				double avg = values.Average();
				//Perform the Sum of (value-avg)_2_2      
				double sum = values.Sum(d => Math.Pow(d - avg, 2));
				//Put it all together      
				stdevOut = Math.Sqrt((sum) / (values.Count() - 1));
			}
			return stdevOut;
		}

		public static List<IdentificationFeatures> recalibrateStDev(List<IdentificationFeatures> idf)
		{
			List<IdentificationFeatures> new_idf = new List<IdentificationFeatures>();
			//double maxStDev = 0.0;
			//// get maximum stdev
			//foreach (IdentificationFeatures i in idf)
			//{
			//    double stdev = i.getStdevConfidenceScore();
			//    if (stdev > maxStDev)
			//    {
			//        maxStDev = stdev;
			//    }
			//}

			// set cardinality of 1 features to max stdev
			foreach (IdentificationFeatures i in idf)
			{
				if (i.getCardinality() == 1)
				{
					i.setStdevConfidenceScore(DEFAULT_STDEV_MAX);
				}
				new_idf.Add(i);
			}
			return new_idf;
		}

		public static DataRow extractFeatureVector(String accession, List<PeptideScore> peptideScores)
		{
			IdentificationFeatures idf = extractFeatures(accession, peptideScores);
			int cardinality = idf.getCardinality();
			Double highestConfidenceScore = idf.getHighestConfidenceScore();
			Double meanConfidenceScore = idf.getMeanConfidenceScore();
			Double medianConfidenceScore = idf.getMedianConfidenceScore();
			Double highestDCN = idf.getHighestDCN();
			Double meanDCN = idf.getMeanDCN();
			Double medianDCN = idf.getMedianDCN();

			#if STDEVINCLUDED
			Double stdevConfidenceScore = idf.getStdevConfidenceScore();
			if (cardinality == 0 || cardinality == 1)
			{
				stdevConfidenceScore = DEFAULT_STDEV_MAX;
				//stdevConfidenceScore = max_stdev;
				//if ((int)Math.Round(stdevConfidenceScore) == 0)
				//{
				//    stdevConfidenceScore = DEFAULT_STDEV_MAX;
				//}
			}
			DataRow r = CreateRow(accession, cardinality, highestConfidenceScore, meanConfidenceScore,
				medianConfidenceScore, highestDCN, meanDCN,medianDCN,
				stdevConfidenceScore);
#else
			DataRow r = CreateRow(accession, cardinality, highestConfidenceScore, meanConfidenceScore,
				medianConfidenceScore, highestDCN, meanDCN, medianDCN);
#endif

			return r;
		}

		public static DataRow CreateRow(params object[] args)
		{
			DataRow r = DataTableSchema.NewRow();
			for (int i = 0; i < TABLE_HEADERS.Length; i++)
			{
				r[TABLE_HEADERS[i]] = (args[i]);
			}
			return r;
		}

		public static DataTable loadDataTable(String fileName)
		{
			DataTable dt = CreateEmptyTable();
			dt.Columns.Add("label", typeof(int));
			return MealTimeMS.IO.Loader.parseIdentificationFeature(fileName, dt);
		}

		//gets the IDataView object from the DataTable, which in turn was , so data 
		public static IDataView transformFeatures(DataTable data, bool isTrainingSet)
		{
			List<String> crossedHeaderNames = new List<string>();
			DataTable crossedData = crossData(data, ref crossedHeaderNames); //cross interaction of features
			
			IDataViewWrapper[] dataViewWrapperSet = new IDataViewWrapper[crossedData.Rows.Count];

			for (int i = 0; i < dataViewWrapperSet.Length; i++)
			{
				//Extract the label and features of the datable into an IDataView Object
				float[] _features = CollectFeatures(crossedData.Rows[i], crossedHeaderNames);
				String accession = (String)crossedData.Rows[i][TABLE_HEADERS[0]];
				if (isTrainingSet)
				{
					int label = (int)crossedData.Rows[i]["label"];
					dataViewWrapperSet[i] = new IDataViewWrapper
					{
						Accession = accession,
						Label = (label == 1),
						Features = _features
					};
				}
				else
				{
					dataViewWrapperSet[i] = new IDataViewWrapper
					{
						Accession = accession,
						Features = _features
					};
				}
			}
			MLContext mlContext = new MLContext();
			//Load Data
			IDataView dv = mlContext.Data.LoadFromEnumerable<IDataViewWrapper>(dataViewWrapperSet);
			//OutputCrossedHeaderNames(crossedHeaderNames);
			return dv;
		}
		private static void OutputCrossedHeaderNames(List<String> crs)
		{
			String header = String.Join("\t", crs);
			String outputFileName = "CrossedHeaderName.txt";
			WriterClass.QuickWrite(header, outputFileName);
			return;
		}

		// Returns Dataset<sql.Row> with the ids in idNames and their pairwise interactions
		// in a vector
		private static DataTable crossData(DataTable data, ref List<String> crossedHeadernNames)
		{

			foreach (String name in FEATURE_LIST)
			{
				crossedHeadernNames.Add(name);
			}
			// compute an interaction between each pair of ids
			for (int i = 0; i < FEATURE_LIST.Length; i++)
			{

				for (int j = i + 1; j < FEATURE_LIST.Length; j++)
				{
					String id1 = FEATURE_LIST[i];
					String id2 = FEATURE_LIST[j];
					String crossName = id1 + " * " + id2;
					crossedHeadernNames.Add(crossName);
					// compute the interaction between id1 and id2 and add this interaction to the list to be assembled
					data.Columns.Add(crossName, typeof(float), crossName);
				}
			}
			return data;
		}
		//collects all features into a floating point vector, a format accepted by the machine learning algorithm 
		private static float[] CollectFeatures(DataRow r, List<String> crossedHeaderNames)
		{
			float[] feature = new float[crossedHeaderNames.Count];
			for (int i = 0; i < feature.Length; i++)
			{
				object obj = r[crossedHeaderNames[i]];
				feature[i] = (float)Convert.ToDouble(obj);
			}
			return feature;
		}


		public static Dictionary<String, Boolean> assessProteinIdentificationConfidence(List<Protein> proteins, ITransformer lrModel)
		{
			MLContext mlContext = new MLContext();
			// Extract the features for the list of proteins
			DataTable proteinVectors = CreateEmptyTable();
			foreach (Protein p in proteins)
			{
				DataRow vector = p.vectorize();
				proteinVectors.Rows.Add(vector.ItemArray);
			}
			// Transform features into a vector for the logistic regression model to predict
			IDataView transformedData = transformFeatures(proteinVectors, false);

			// Predictions of protein identification confidence predicted by model
			IDataView identificationPredictions = lrModel.Transform(transformedData);


			//convert from IDatabiew to an IEnumerable
			IEnumerable<ProteinPrediction> predictionList = mlContext.Data.
				CreateEnumerable<ProteinPrediction>(identificationPredictions, reuseRowObject: true);

			// Collect the protein accession and the identification prediction
			Dictionary<String, Boolean> predictions = new Dictionary<String, Boolean>();
			foreach (ProteinPrediction r in predictionList)
			{
				String accession = (String)r.Accession;
				Boolean prediction = r.Probability >= GlobalVar.AccordThreshold;//TODO shouldn't be accord threshold, but this function is depricated anyways
				predictions.Add(accession, prediction);
			}
			return predictions;
		}
		public static Dictionary<String, Boolean> assessProteinIdentificationConfidence(List<Protein> proteins, LogisticRegression lrModel)
		{
			//transform basic features to crossed features
			IDataView transformedData = ProteinListToTransformedData(proteins);
			
			//pass features into the classifier
			bool[] identificationPredictions = IdentificationLogisticRegressionTrainer.AccordDecide(lrModel, transformedData, GlobalVar.AccordThreshold);

			// Collect the protein accession and the identification prediction
			Dictionary<String, Boolean> predictions = JoinAccessionWithPrediction(proteins, identificationPredictions);
			return predictions;
		}
		public static Dictionary<String, Boolean> assessProteinIdentificationConfidence(List<Protein> proteins, SupportVectorMachine<Gaussian> svmModel)
		{
			//transform basic features to crossed features
			IDataView transformedData = ProteinListToTransformedData(proteins);

			//pass features into the classifier
			bool[] identificationPredictions = IdentificationLogisticRegressionTrainer.SVMDecide(svmModel, transformedData);

			// Collect the protein accession and the identification prediction
			Dictionary<String, Boolean> predictions = JoinAccessionWithPrediction(proteins, identificationPredictions);
			return predictions;
		}

		private static IDataView ProteinListToTransformedData(List<Protein> proteins)
		{
			// Extract the features for the list of proteins
			DataTable proteinVectors = CreateEmptyTable();
			foreach (Protein p in proteins)
			{
				DataRow vector = p.vectorize();

				proteinVectors.Rows.Add(vector.ItemArray);
			}
			// Transform features into a vector for the classifier model to predict
			IDataView transformedData = transformFeatures(proteinVectors, false);
			return transformedData;
		}

		private static Dictionary<String, Boolean> JoinAccessionWithPrediction(List<Protein> proteins, bool[] identificationPredictions)
		{
			// Collect the protein accession and the identification prediction
			Dictionary<String, Boolean> predictions = new Dictionary<String, Boolean>();
			for (int i = 0; i < proteins.Count; i++)
			{
				String accession = (String)proteins[i].getAccession();
				Boolean prediction = identificationPredictions[i];
				predictions.Add(accession, prediction);
			}
			return predictions;
		}

	}
	public class IDataViewWrapper
	{
#if STDEVINCLUDED
		[VectorType(15)]
#else
		[VectorType(10)]
#endif
		[ColumnName("Features")]
		public float[] Features { get; set; }


		[ColumnName("Label")]
		public bool Label { get; set; }

		public string Accession { get; set; }



	}
	public class ProteinPrediction : IDataViewWrapper
	{
		[ColumnName("PredictedLabel")]
		public bool Prediction { get; set; }

		[ColumnName("Probability")]
		public float Probability { get; set; }

		[ColumnName("Score")]
		public float Score { get; set; }
	}
	public class SVMProteinPrediction : IDataViewWrapper
	{
		[ColumnName("PredictedLabel")]
		public bool Prediction { get; set; }

		[ColumnName("Score")]
		public float Score { get; set; }
	}

}

