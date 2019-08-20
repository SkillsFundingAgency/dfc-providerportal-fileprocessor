using System.IO;

namespace Dfc.ProviderPortal.FileProcessor.Provider.Test.Unit.Helpers
{
    public static class CsvStreams
    {
        public static Stream BulkUpload_ValidMultiple()
        {
            MemoryStream ms = new MemoryStream();

            TextWriter sw = new StreamWriter(ms);

            sw.WriteLine("LARS_QAN,WHO_IS_THIS_COURSE_FOR,ENTRY_REQUIREMENTS,WHAT_YOU_WILL_LEARN,HOW_YOU_WILL_LEARN,WHAT_YOU_WILL_NEED_TO_BRING,HOW_YOU_WILL_BE_ASSESSED,WHERE_NEXT,ADVANCED_LEARNER_OPTION,ADULT_EDUCATION_BUDGET,COURSE_NAME,ID,DELIVERY_MODE,START_DATE,FLEXIBLE_START_DATE,VENUE,NATIONAL_DELIVERY,REGION,SUB_REGION,URL,COST,COST_DESCRIPTION,DURATION,DURATION_UNIT,STUDY_MODE,ATTENDANCE_PATTERN");
            sw.WriteLine("60333078,aaaaa,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,A COURSE, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("60333078,,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,B COURSE, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("60333078,,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,C COURSE, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("60333078,,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,D COURSE, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("60333078,,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,E COURSE, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("50095018,aaaaa,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,A COURSE, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("50095018,aaaaa,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,B COURSE, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("50095018,aaaaa,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,C COURSE, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("50095018,aaaaa,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,D COURSE, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("5010830X,GHFHFHF,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,A TEST, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("5010830X,,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,B TEST, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("5010830X,,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,C TEST, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("5010830X,,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,D TEST, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("50097453,aaaaa,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,VALID COURSE, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("50097453,aaaaa,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,VALID COURSE, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("50097453,aaaaa,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,VALID COURSE, test001, Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("60333078,aaaaa,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,AA,test001,Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("60333078,aaaaa,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,BB,test001,Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("60333078,aaaaa,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,CC,test001,Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;
            sw.WriteLine("60333078,aaaaa,bbbb,cccc,dddd,eeee,ffff,gggg,No,No,DD,test001,Classroom based,27/04/2020,Yes,TEST,,,,www.bbc.co.uk,\"99,999.00\",\"A course for people with no previous experience in counselling training, but who have an interest in counselling or who want to use counselling skills in their work. Practical training in interpersonal and counselling skills, an exploration of professiKKK\",6,Months,full-time,DAYTIME") ;

            sw.Flush();

            return ms;
        }

        public static Stream GetInvalidPublisherCsvStream()
        {
            MemoryStream ms = new MemoryStream();

            TextWriter sw = new StreamWriter(ms);
            sw.WriteLine("Col One, , Col Three");
            sw.WriteLine("v1, v2, v3");
            sw.WriteLine("v4, v5, v6");
            sw.WriteLine("v7, v8, v9");
            sw.Flush();

            return ms;
        }
    }
}
