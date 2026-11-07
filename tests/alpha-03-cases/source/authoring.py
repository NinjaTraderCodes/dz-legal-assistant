# Codex-authored fictitious source data. P/B/E/R/L/Q/D/C only assemble annotations;
# they do not read model answers or infer labels from Arabic phrases.

P('f01', ('loan','lender'), [
    L('role','المدعي مقرض والمدعى عليه مقترض، واتفقا شفويا على قرض دون فائدة'),
    L('loan','سلم المدعي للمدعى عليه 840.000 دج في 14/02/2023'),
    L('due','انتهى أجل الرد في 14/08/2023، وبقي أصل القرض كله دون سداد'),
    L('side','لم يتفق الطرفان على رهن، واحتفظ المدعي برسالة تتعلق بمكان اللقاء فقط'),
    Q('pay','يلتمس إلزام المدعى عليه برد 840.000 دج')],
  [E('money_lent',['loan'],actor='plaintiff',date='14/02/2023',amount='840000'), E('deadline_passed',['due'],date='14/08/2023'), E('payment_made',['due'],polarity='denied',actor='defendant'), E('debt_unpaid',['loan','due'],actor='defendant',amount='840000',amount_cites=['loan'])],
  [R('payment',['pay'],'840000')], [
    C('وجّه المدعي إعذارا للمقترض في 03/09/2023، ولم يدفع هذا الأخير رغم تبليغه',actor='plaintiff',date='03/09/2023'),
    C('لم يوجه المدعي أي إعذار أو مطالبة سابقة إلى المقترض',actor='plaintiff'),
    C('ينكر المقترض أن المدعي وجّه إليه إعذارا في 03/09/2023',actor='plaintiff',date='03/09/2023',note='Party contest; plaintiff reliance implicit, not notes-level denial'),
    C(None)])

P('f02', ('loan','lender'), [
    L('role','المدعية أقرضت المدعى عليه مبلغا نقديا ولم تكن مدينة له'),
    L('loan','جرى تسليم 615.000 دج للمقترض يوم 19/06/2022'),
    L('due','حل أجل رد القرض يوم 19/12/2022؛ تفيد الموكلة بأنه لم يرجع أي جزء منه'),
    D('side','نسخة وصل إيداع ملف المكتب رقم 90741 بتاريخ 06/01/2023'),
    Q('pay','تطلب المدعية استرداد أصل القرض البالغ 615.000 دج')],
  [E('money_lent',['loan'],actor='plaintiff',date='19/06/2022',amount='615000'),E('deadline_passed',['due'],date='19/12/2022'),E('payment_made',['due'],polarity='denied',actor='defendant'),E('debt_unpaid',['loan','due'],actor='defendant',amount='615000',amount_cites=['loan'])],
  [R('payment',['pay'],'615000')],[
    C('حرر المقترض اعترافا بالدين قدره 615.000 دج في 28/06/2022',actor='defendant',date='28/06/2022',amount='615000'),
    C('لم يحرر المقترض اعترافا بالدين، لا في محرر مستقل ولا في رسالة',actor='defendant'),
    C(['تتمسك المدعية باعتراف حرره المقترض في 28/06/2022 بدين قدره 615.000 دج','أما المدعى عليه فينكر أنه صدر عنه ذلك الاعتراف'],actor='defendant',date='28/06/2022',amount='615000',note='Assertion and contest in separate sentences'),
    C(None)])

P('f03', ('sale','seller'), [
    L('role','المدعي باع للمدعى عليه أثاث مكتب مستعملا وهو يطالب بصفته البائع'),
    L('sale','أبرم البيع في 08/04/2024 بثمن 275.000 دج'),
    L('handover','سلّم البائع الأثاث يوم 11/04/2024، ولم يسدد المشتري شيئا من الثمن'),
    Q('pay','المطلوب إلزام المشتري بأداء الثمن، أي 275.000 دج')],
  [E('contract_concluded',['sale'],date='08/04/2024',amount='275000'),E('goods_delivered',['handover'],actor='plaintiff',date='11/04/2024'),E('payment_made',['handover'],polarity='denied',actor='defendant'),E('price_unpaid',['sale','handover'],actor='defendant',amount='275000',amount_cites=['sale'])],
  [R('payment',['pay'],'275000')],[
    C('بلغ المدعي مشتري الأثاث مطالبة بالدفع في 29/04/2024',actor='plaintiff',date='29/04/2024'),
    C('تؤكد ملاحظات المكتب أنه لم تسبق الدعوى أية مطالبة أو إعذار بالدفع',actor='plaintiff'),
    C('يقر المشتري بتسلم الأثاث لكنه يجحد حصول المطالبة التي يقول البائع إنه بلغها في 29/04/2024',actor='plaintiff',date='29/04/2024'),
    C(None)])

P('f04', ('sale','seller'), [
    L('role','المدعية هي بائعة آلة خياطة للمدعى عليه؛ الخلاف في حساب الثمن'),
    L('sale','تم البيع في 22/05/2024 بثمن 460.000 دج'),
    L('delivery','سلمت البائعة الآلة يوم 24/05/2024'),
    L('balance','يتفق الطرفان على أن الثمن لم يسدد كاملا، ويتنازعان في الحساب'),
    Q('pay','تطلب المدعية إلزام المشتري بدفع المستحق بعد تصفية الحساب')],
  [E('contract_concluded',['sale'],date='22/05/2024',amount='460000'),E('goods_delivered',['delivery'],actor='plaintiff',date='24/05/2024'),E('price_unpaid',['balance'],actor='defendant')],
  [R('payment',['pay'])],[
    C('سدد المشتري قسطا من الثمن في 30/05/2024',actor='defendant',date='30/05/2024'),
    C('لم يسدد المشتري أي قسط من الثمن',actor='defendant'),
    C('يتمسك المشتري بأنه دفع قسطا في 30/05/2024، وتعترض البائعة على وقوع هذا الدفع',actor='defendant',date='30/05/2024'),
    C(None)])

P('f05', ('works_contract','client'), [
    L('role','المدعي رب عمل كلف المدعى عليه المقاول بتهيئة محل تجاري'),
    L('contract','وقعا عقد الأشغال يوم 16/01/2024 بقيمة 980.000 دج'),
    L('stop','توقف المقاول عن العمل نهائيا قبل إنجاز السقف، ولم يستأنف الورشة'),
    L('side','لم يسلم رب العمل مفاتيح المخزن المجاور لأنه لا يدخل في الأشغال المتفق عليها'),
    Q('end','يلتمس المدعي فسخ عقد الأشغال')],
  [E('contract_concluded',['contract'],date='16/01/2024',amount='980000'),E('work_stopped',['stop'],actor='defendant')],
  [R('rescission',['end'])],[
    C('أنذر رب العمل المقاول بالرجوع إلى الورشة في 07/03/2024',actor='plaintiff',date='07/03/2024'),
    C('لم يحصل إعذار للمقاول قبل فتح الملف، ولم توجه له مطالبة بالرجوع إلى العمل',actor='plaintiff'),
    C(['يذكر رب العمل إنذارا بالرجوع إلى الورشة مؤرخا في 07/03/2024','يرد المقاول بأن ذلك الإنذار لم يوجه إليه أصلا'],actor='plaintiff',date='07/03/2024'),
    C(None)])

P('f06', ('works_contract','client'), [
    L('role','المدعية صاحبة مشروع نجارة والمدعى عليه المقاول المكلف بصنع خزائن لها'),
    L('contract','اتفقا في 09/09/2023 على أشغال بثمن 720.000 دج يستحق أصله عند اكتمالها'),
    L('stop','هجر المقاول الورشة قبل إتمام الأشغال، ولذلك تطلب صاحبة المشروع الفسخ'),
    Q('end','تلتمس فسخ عقد المقاولة وتعويضا قدره 85.000 دج')],
  [E('contract_concluded',['contract'],date='09/09/2023',amount='720000'),E('work_stopped',['stop'],actor='defendant')],
  [R('rescission',['end']),R('damages',['end'],'85000')],[
    C('رغم شرط الاستحقاق دفعت صاحبة المشروع تسبيقا قدره 120.000 دج في 14/09/2023',actor='plaintiff',date='14/09/2023',amount='120000',extra=[Q('refund','تطلب كذلك رد التسبيق 120.000 دج')],requests=[R('restitution',['refund'],'120000')]),
    C('لم تدفع صاحبة المشروع أي مبلغ للمقاول',actor='plaintiff'),
    C('تقول صاحبة المشروع إنها سلمت 120.000 دج في 14/09/2023، بينما ينكر المقاول حصول هذا التسبيق',actor='plaintiff',date='14/09/2023',amount='120000',extra=[Q('refund','تتمسك بطلب رد التسبيق المتنازع عليه وقدره 120.000 دج')],requests=[R('restitution',['refund'],'120000')]),
    C(None)])

P('f07', ('sale','buyer'), [
    L('role','المدعي مشتري جهاز تبريد من المدعى عليه البائع'),
    L('sale','أبرم بيع الجهاز في 12/10/2024 بثمن 1.350.000 دج'),
    L('fraud','أخفى البائع عن المشتري تعديلا في محرك الجهاز'),
    Q('end','يلتمس إبطال البيع وتعويضا عن التدليس قدره 95.000 دج')],
  [E('contract_concluded',['sale'],date='12/10/2024',amount='1350000'),E('concealment',['fraud'],actor='defendant')],
  [R('annulment',['end']),R('damages',['end'],'95000')],[
    C('دفع المشتري 350.000 دج من الثمن في 13/10/2024',actor='plaintiff',date='13/10/2024',amount='350000',extra=[Q('refund','ويطلب إعادة ما دفعه، أي 350.000 دج')],requests=[R('restitution',['refund'],'350000')]),
    C('لم يدفع المشتري شيئا من الثمن',actor='plaintiff'),
    C('يجحد البائع ما يتمسك به المشتري من تسديد 350.000 دج يوم 13/10/2024',actor='plaintiff',date='13/10/2024',amount='350000',extra=[Q('refund','يطلب المشتري استرداد المبلغ الذي يؤكد دفعه وقدره 350.000 دج')],requests=[R('restitution',['refund'],'350000')]),
    C(None)])

def V(lines, events, requests, values=(), note=''):
    return dict(lines=lines, events=events, requests=requests, valueNotes=list(values), note=note)

def N(kind, value, purpose, cites):
    return dict(kind=kind, value=value, purpose=purpose, evidence=cites)

def fraud_binding(sale, pay, detail, ask, sd, price, pd, paid, damages, refund=True, doc=False, values=()):
    return V([L('role','المدعية اشترت من المدعى عليه آلة تعبئة وتتمسك بإبطال البيع للتدليس'),
              L('sale',sale), (D if doc else L)('pay',pay), L('fraud',detail),
              L('context1','كانت الآلة مخصصة لتعبئة منتجات الورشة الصغيرة التي تديرها المدعية، وقد وضعتها في المخزن إلى أن يحضر تقني مستقل لمعاينة حالتها، ولم يكن عامل المخزن طرفا في اتفاق البيع'),
              L('context2','تبادلت المدعية مع البائع مراسلات عن مكان تركيب الجهاز وطريقة نقله، ثم طلبت منه السماح للتقني بمراجعة سجل الصيانة، وكان جوابه أن السجل بقي لدى متعامل سابق وأنه سيبحث عنه'),
              L('context3','أرسلت المدعية إلى المكتب صور لوحات الجهاز ومراسلات الورشة، وتقول إن الاطلاع على سجل الصيانة جعلها تعيد النظر في الصفقة وتتمسك بالتدليس'),Q('ask',ask)],
             [E('contract_concluded',['sale'],date=sd,amount=price),E('payment_made',['pay'],actor='plaintiff',date=pd,amount=paid),E('concealment',['fraud'],actor='defendant')],
             [R('annulment',['ask']),R('damages',['ask'],damages)]+([R('restitution',['ask'],paid)] if refund else []), values)

B('f08',('sale','buyer'),[
    fraud_binding('وقع البيع في 14/03/2024 بثمن 800.000 دج','دفعت المشترية 480.000 دج في 21/03/2024',
      'أخفى البائع أن عداد الآلة قد استبدل قبل البيع؛ تاريخ اكتشاف ذلك في تقرير الخبير هو 28/04/2024',
      'تلتمس إبطال البيع ورد 480.000 دج وتعويضا قدره 130.000 دج', '14/03/2024','800000','21/03/2024','480000','130000',
      values=[N('date','28/04/2024','discovery/report date, not sale or payment',['fraud'])]),
    fraud_binding('أبرم عقد البيع في 17/02/2023 مقابل 900.000 دج','إيصال دفع كامل الثمن 900.000 دج يوم 20/02/2023؛ صورة الإيصال استخرجت في 11/04/2024',
      'تعمد البائع إخفاء عطب جوهري في وحدة التشغيل؛ تقدر المدعية خسارة الانتفاع الشهرية بـ12.000 دج',
      'الطلبات: إبطال البيع وإرجاع 900.000 دج، وتعويض قدره 145.000 دج', '17/02/2023','900000','20/02/2023','900000','145000',doc=True,
      values=[N('date','11/04/2024','copy issue date',['pay']),N('amount','12000','monthly loss, not requested damages total',['fraud'])]),
    fraud_binding('يعود إبرام البيع إلى 06/05/2024 بثمن 1.100.000 دج','في 09/05/2024 دفعت المشترية تسبيقا قدره 300.000 دج',
      'أخفى البائع عدم صلاحية مضخة الآلة؛ فاتورة الإصلاح التقديرية 45.000 دج',
      'تطلب الإبطال ورد التسبيق 300.000 دج، وتعويضا عن مجمل الضرر قدره 160.000 دج', '06/05/2024','1100000','09/05/2024','300000','160000',
      values=[N('amount','45000','repair estimate, not requested damages',['fraud'])]),
    fraud_binding('بيع الآلة مؤرخ في 02/12/2022 وقيمته 950.000 دج','أدت المدعية من الثمن 650.000 دج بتاريخ 05/12/2022',
      'تقول الملاحظات إن البائع أخفى استهلاك المحرك؛ رقم ملف الصيانة 88410',
      'تلتمس إبطال البيع وتعويضا قدره 210.000 دج فقط', '02/12/2022','950000','05/12/2022','650000','210000',refund=False,
      values=[N('identifier','88410','maintenance file identifier',['fraud'])])])

P('f09', ('transport_contract','client'), [
    L('role','المدعي زبون تعاقد مع المدعى عليه لنقل صناديق زجاج إلى ورشته'),
    L('contract','انعقد اتفاق النقل يوم 13/07/2024 مقابل أجرة نقل 68.000 دج'),
    L('breach','وصلت الصناديق في الموعد لكنها مكسورة بسبب إخلال الناقل بطريقة التثبيت'),
    Q('dam','يلتمس المدعي تعويضا عن الزجاج المتلف مقداره 420.000 دج')],
  [E('contract_concluded',['contract'],date='13/07/2024',amount='68000'),E('other',['breach'],actor='defendant'),E('delivery_late',['breach'],polarity='denied',actor='defendant'),E('delivery_missing',['breach'],polarity='denied',actor='defendant')],
  [R('damages',['dam'],'420000')],[
    C('طالب الزبون الناقل رسميا بالتعويض في 25/07/2024',actor='plaintiff',date='25/07/2024'),
    C('لم يسبق للزبون توجيه مطالبة أو إعذار إلى الناقل بشأن التلف',actor='plaintiff'),
    C('يعارض الناقل قول الزبون إنه أنذره بالتعويض يوم 25/07/2024، وينكر توجيه ذلك الإنذار',actor='plaintiff',date='25/07/2024'),
    C(None)])

P('f10', ('sale','buyer'), [
    L('role','المدعية اشترت من المدعى عليه تجهيزات إنارة لمحلها'),
    L('contract','عقد التجهيزات موقع في 04/01/2025 بثمن 560.000 دج'),
    L('defect','سلمت التجهيزات فعلا لكنها مخالفة للمواصفات المتفق عليها'),
    Q('dam','تطلب تعويضا قدره 175.000 دج عن عدم المطابقة')],
  [E('contract_concluded',['contract'],date='04/01/2025',amount='560000'),E('delivery_missing',['defect'],polarity='denied',actor='defendant'),E('other',['defect'],actor='defendant')],
  [R('damages',['dam'],'175000')],[
    C('تم التسليم متأخرا في 18/02/2025 بعدما كان موعده 01/02/2025',actor='defendant',date='18/02/2025'),
    C('لا يوجد تأخر في التسليم؛ فقد جرى في موعده',actor='defendant'),
    C(['تقول المشترية إن التسليم تأخر إلى 18/02/2025','يرفض البائع وصفه بالتأخر ويقول إنه سلم في الموعد المعدل'],actor='defendant',date='18/02/2025'),
    C(None)])

P('f11', ('none','victim'), [
    L('role','المدعي مضرور يطالب المدعى عليه شخصيا بالتعويض عن الاعتداء عليه'),
    L('harm','اعتدى المدعى عليه على المدعي يوم 17/08/2023 وسبب له إصابة في يده'),
    L('side','يطلب المضرور التعويض عن الإصابة'),
    Q('dam','طلبه المدني هو تعويض قدره 230.000 دج')],
  [E('harm_by_person',['harm'],actor='defendant',date='17/08/2023')],[R('damages',['dam'],'230000')],[
    C('صدر حكم جزائي بإدانة المدعى عليه عن الاعتداء بتاريخ 04/12/2023',date='04/12/2023'),
    C('لم يصدر حكم جزائي بإدانة المدعى عليه عن هذا الاعتداء'),
    C('يتمسك المضرور بحكم إدانة مؤرخ في 04/12/2023، والمدعى عليه ينازع في صدور هذا الحكم أصلا',date='04/12/2023'),
    C(None)])

def tort_binding(harm, judgment, ask, hd, jd, amount, extra, values=()):
    lines=[L('role','المدعية هي المصابة والمدعى عليه هو الشخص الذي أحدث الإصابة'),L('harm',harm),L('extra',extra),Q('ask',ask)]
    events=[E('harm_by_person',['harm'],actor='defendant',date=hd)]
    if judgment:
        lines.append(D('judgment',judgment));events.append(E('criminal_conviction',['judgment'],date=jd))
    return V(lines,events,[R('damages',['ask'],amount)],values)

B('f12',('none','victim'),[
    tort_binding('دفع المدعى عليه المدعية بعنف يوم 07/06/2022 فأصيبت في كتفها',
      'حكم جزائي بإدانة المعتدي صادر في 19/10/2022؛ نسخة تنفيذية مستخرجة في 08/02/2023',
      'تطلب تعويضا عن الضرر قدره 390.000 دج','07/06/2022','19/10/2022','390000',
      'بلغت مصاريف العلاج المدونة في الفواتير 46.000 دج',
      [N('date','08/02/2023','judgment copy date',['judgment']),N('amount','46000','treatment expenses, not total damages',['extra'])]),
    tort_binding('وقع الاعتداء من المدعى عليه في 23/09/2023 عندما ضرب المدعية عند مدخل الورشة',
      'حكم الإدانة بتاريخ 15/01/2024، ورقم قيده 62183',
      'تلتمس تعويضا قدره 510.000 دج','23/09/2023','15/01/2024','510000',
      'قدر توقف الدخل الشهري بـ32.000 دج، والتقرير الطبي محرر في 24/09/2023',
      [N('identifier','62183','judgment registry number',['judgment']),N('amount','32000','monthly income loss',['extra']),N('date','24/09/2023','medical report date',['extra'])]),
    tort_binding('أحدث المدعى عليه إصابة المدعية في 02/03/2024؛ أثبت الطبيب المعاينة في 05/03/2024',
      'حكم جزائي أدان المدعى عليه في 27/11/2024',
      'تطلب المدعية 285.000 دج تعويضا','02/03/2024','27/11/2024','285000',
      'يوجد وصل دواء بمبلغ 18.000 دج',
      [N('date','05/03/2024','medical examination, not incident date',['harm']),N('amount','18000','medicine receipt, not damages total',['extra'])]),
    tort_binding('أصاب المدعى عليه المدعية عمدا بتاريخ 09/05/2025',None,
      'يلتمس دفاع المدعية تعويضا قدره 640.000 دج','09/05/2025',None,'640000',
      'فتح ملف الشكوى يوم 12/05/2025 تحت رقم 77429',
      [N('date','12/05/2025','complaint filing date, not conviction',['extra']),N('identifier','77429','complaint identifier',['extra'])])])

P('f13', ('none','victim'), [
    L('role','المدعي عامل توصيل يطالب المدعى عليه صاحب المستودع بالتعويض بصفته مضرورا'),
    L('cart','انفلتت عربة يدوية يحرسها المدعى عليه يوم 16/04/2024 وأتلفت دراجة المدعي'),
    Q('dam','يلتمس تعويضا عن الأضرار التي يؤكدها في الملف قدره 310.000 دج')],
  [E('harm_by_thing',['cart'],date='16/04/2024')],[R('damages',['dam'],'310000')],[
    C('وعض كلب في حراسة صاحب المستودع المدعي أيضا يوم 19/04/2024',date='19/04/2024'),
    C('لم يصب المدعي بأي ضرر من كلب أو حيوان'),
    C('يؤكد المدعي أن كلب صاحب المستودع عضه في 19/04/2024، وينكر صاحب المستودع واقعة العضة',date='19/04/2024'),
    C(None)])

P('f14', ('none','victim'), [
    L('role','المدعية تضررت في ملحق ورشة يملكه المدعى عليه ويتولى حراسة معداته'),
    L('machine','أفسدت آلة رفع تابعة للمدعى عليه بضاعة المدعية يوم 11/11/2023'),
    L('side','لم يحصل حريق في المخزن'),
    Q('dam','تطلب تعويضا عن الضرر الذي تتمسك به بمقدار 760.000 دج')],
  [E('harm_by_thing',['machine'],date='11/11/2023')],[R('damages',['dam'],'760000')],[
    C('سقط كذلك جزء من جدار الملحق الذي يملكه المدعى عليه يوم 18/11/2023 فأصاب بضاعة المدعية',date='18/11/2023'),
    C('لم يقع انهيار بناء أو سقوط جدار'),
    C('تضيف المدعية أن جدار الملحق انهار على بضاعتها في 18/11/2023، لكن المالك ينفي الانهيار المنسوب إلى مبناه',date='18/11/2023'),
    C(None)])

P('f15', ('lease','lessor'), [
    L('role','المدعي مؤجر شقة والمدعى عليه مستأجرها'),
    L('lease','وقع الإيجار في 03/02/2023 ببدل شهري 28.000 دج'),
    L('start','بدأ انتفاع المستأجر بالشقة في 10/02/2023'),
    L('arrears','لم يسدد المستأجر بدلات إيجار قدرها 168.000 دج'),
    Q('pay','يطلب المؤجر دفع المتأخرات، أي 168.000 دج، وإخلاء الشقة')],
  [E('contract_concluded',['lease'],date='03/02/2023',amount='28000'),E('lease_started',['start'],date='10/02/2023'),E('rent_unpaid',['arrears'],actor='defendant',amount='168000')],
  [R('payment',['pay'],'168000'),R('eviction',['pay'])],[
    C('محضر توجيه المؤجر إعذارا بالدفع للمستأجر في 26/09/2023',actor='plaintiff',date='26/09/2023',field='documents.list',note='Named document alone affirms its event'),
    C('لم يصدر عن المؤجر أي إعذار أو مطالبة سابقة بالدفع للمستأجر',actor='plaintiff'),
    C('لا ينازع المستأجر في تاريخ بدء الإيجار لكنه ينكر أن المؤجر وجه له إعذارا بالدفع في 26/09/2023',actor='plaintiff',date='26/09/2023'),
    C(None)])

def rent_binding(cd, start, monthly, arrears, deposit, nd, pay, eviction, end, doc=False):
    requested=([f'دفع {pay} دج من المتأخرات'] if pay else [])+(['إخلاء المسكن'] if eviction else [])+(['فسخ الإيجار'] if end else [])
    ask='تطلب '+' و'.join(requested)+('؛ مع حفظ حقها في المطالبة بالبدلات في دعوى أخرى' if not pay else '')
    lines=[L('role','المدعية مؤجرة المسكن والمدعى عليه مستأجره'),
           L('lease',f'عقد إيجار مؤرخ في {cd}، والكراء {monthly} دج كل شهر؛ دفع المستأجر {deposit} دج ضمانا'),
           L('start',f'تسلم المستأجر المسكن وبدأ انتفاعه به في {start}'),
           L('arrears',f'بقيت في ذمة المستأجر بدلات إيجار قدرها {arrears} دج'),
           L('context1','الشقة تقع في عمارة تسكن المدعية في طابق آخر منها، وكان المستأجر يتسلم المفاتيح من الحارس عند غياب المؤجرة'),
           L('context2','اشتكى المستأجر من قراءة عداد الماء ومن إصلاح قفل الباب المشترك، وردت المؤجرة بأن هذه المسائل تعالج مع مسير العمارة'),
           L('context3','احتفظت المؤجرة بإيصالات تخص فترات مدفوعة سابقة وأرفقتها بالكشف الحالي'),
           (D if doc else L)('notice',f'محضر توجيه المؤجرة إعذارا بالدفع للمستأجر في {nd}؛ رقم المحضر 93517'),
           Q('ask',ask)]
    return V(lines,[E('contract_concluded',['lease'],date=cd,amount=monthly),E('lease_started',['start'],date=start),E('rent_unpaid',['arrears'],actor='defendant',amount=arrears),E('notice_served',['notice'],actor='plaintiff',date=nd)],
             ([R('payment',['ask'],pay)] if pay else [])+([R('eviction',['ask'])] if eviction else [])+([R('rescission',['ask'])] if end else []),
             [N('amount',deposit,'security deposit, not rent or arrears',['lease']),N('identifier','93517','service record identifier',['notice'])],
             'Monthly contractual rate and expressly stated arrears are distinct; no arithmetic supplies a value')

B('f16',('lease','lessor'),[
    rent_binding('18/09/2022','01/10/2022','34000','204000','68000','17/07/2023','204000',True,False),
    rent_binding('07/03/2023','15/03/2023','45000','315000','90000','04/12/2023','225000',False,True),
    rent_binding('26/01/2024','01/02/2024','26000','156000','52000','19/09/2024','156000',True,False,doc=True),
    rent_binding('11/05/2023','20/05/2023','38000','228000','76000','08/02/2024',None,True,True)])

P('f17', ('ownership','owner'), [
    L('role','المدعي مالك قطعة أرض والمدعى عليه يشغلها دون أن يكون مستأجرا منه'),
    L('occupation','دخل المدعى عليه الأرض يوم 24/03/2022 وبقي فيها دون عقد أو سند'),
    L('side','لا توجد علاقة إيجار بين المالك والشاغل'),
    Q('out','يلتمس طرد الشاغل من العقار')],
  [E('occupation_without_title',['occupation'],actor='defendant',date='24/03/2022')],[R('eviction',['out'])],[
    C('طلب المالك من الشاغل الخروج بإعذار مؤرخ في 06/06/2022',actor='plaintiff',date='06/06/2022'),
    C('لم يطالب المالك الشاغل بالخروج قبل هذه الدعوى ولم يوجه إليه إعذارا',actor='plaintiff'),
    C(['يسند المالك طلبه إلى إعذار بالخروج في 06/06/2022','ينكر الشاغل أن هذا الإعذار وجه إليه'],actor='plaintiff',date='06/06/2022'),
    C(None)],court='القسم العقاري')

def occupation_binding(ownership, occupation, notice, ask, od, nd, damage, values, court='القسم العقاري'):
    return V([L('role','المدعية مالكة العقار، والمدعى عليه شاغل ليس بينه وبينها عقد إيجار'),
              D('title',ownership),L('occupation',occupation),D('notice',notice),Q('ask',ask)],
             [E('occupation_without_title',['occupation'],actor='defendant',date=od),E('notice_served',['notice'],actor='plaintiff',date=nd)],
             [R('eviction',['ask'])]+([R('damages',['ask'],damage)] if damage else []),values)

B('f18',('ownership','owner'),[
    occupation_binding('عقد ملكية المدعية مؤرخ في 12/04/2016؛ رقم القسم المساحي 80651',
      'استولى المدعى عليه على المكان في 09/08/2023 وهو يشغله دون أي سند',
      'محضر مطالبة المالكة للشاغل بالخروج يوم 22/08/2023؛ قيد النسخة في المكتب يوم 05/09/2023',
      'تطلب الطرد وتعويضا عن الشغل غير المشروع قدره 170.000 دج','09/08/2023','22/08/2023','170000',
      [N('date','12/04/2016','ownership acquisition, not occupation',['title']),N('identifier','80651','cadastral identifier',['title']),N('date','05/09/2023','copy registration date',['notice'])]),
    occupation_binding('سند ملكية في 03/07/2019، مجموعة الملكية رقم 37420',
      'بدأ شغل المدعى عليه غير المستند إلى عقد في 18/01/2024؛ يقدر الانتفاع الشهري بالمكان بـ9.000 دج',
      'إعذار بالخروج وجهته المدعية يوم 14/02/2024',
      'تلتمس الطرد و180.000 دج تعويضا','18/01/2024','14/02/2024','180000',
      [N('date','03/07/2019','ownership date',['title']),N('identifier','37420','property group identifier',['title']),N('amount','9000','monthly use estimate',['occupation'])]),
    occupation_binding('أصل الملكية مؤرخ في 27/11/2015، والنسخة المستخرجة في 11/03/2025',
      'أقام المدعى عليه سياجا وشغل أرض المدعية بلا سند منذ 06/12/2024',
      'محضر توجيه المدعية مطالبة بإخلاء الأرض بتاريخ 09/01/2025',
      'تلتمس طرد الشاغل فقط','06/12/2024','09/01/2025',None,
      [N('date','27/11/2015','title date',['title']),N('date','11/03/2025','title copy issue date',['title'])]),
    occupation_binding('عقد الملكية يوم 15/02/2018، ورقم حفظه 91268',
      'يشغل المدعى عليه العقار دون عقد ابتداء من 20/09/2022؛ قدرت المدعية الانتفاع الشهري بـ4.500 دج',
      'إعذار بالخروج من المالكة إلى الشاغل يوم 07/10/2022',
      'تطلب الطرد وتعويضا قدره 430.000 دج عن الضرر','20/09/2022','07/10/2022','430000',
      [N('date','15/02/2018','title date',['title']),N('identifier','91268','title registration number',['title']),N('amount','4500','monthly use rate, not requested total',['occupation'])])],court='القسم العقاري')

P('f19', ('supply','supplier'), [
    L('role','المدعية موردة مواد تعبئة والمدعى عليه تاجر اشترى منها لحساب محله'),
    L('supply','سلمت المدعية البضائع في 02/06/2024 بقيمة 685.000 دج'),
    L('debt','لم يدفع التاجر أي مبلغ من قيمة هذه البضائع وبقيت قيمتها كاملة مستحقة'),
    D('side','نسخة كشف تحميل مؤرخ في 31/05/2024 ورقم أمر الشحن 50829'),
    Q('pay','تطلب إلزام التاجر بدفع 685.000 دج')],
  [E('goods_delivered',['supply'],actor='plaintiff',date='02/06/2024',amount='685000'),E('payment_made',['debt'],polarity='denied',actor='defendant'),E('price_unpaid',['supply','debt'],actor='defendant',amount='685000',amount_cites=['supply'])],
  [R('payment',['pay'],'685000')],[
    C('وجهت الموردة للتاجر مطالبة بأداء الفواتير في 18/07/2024',actor='plaintiff',date='18/07/2024'),
    C('لم توجه الموردة أية مطالبة أو إعذار بالأداء قبل فتح الملف',actor='plaintiff'),
    C('يدعي التاجر أن الموردة لم توجه مطالبتها المؤرخة في 18/07/2024، مع تمسكها بتوجيهها',actor='plaintiff',date='18/07/2024'),
    C(None)],court='القسم التجاري')

P('f20', ('supply','supplier'), [
    L('role','المدعي مورد قطع ميكانيكية والمدعى عليه عميل تاجر؛ الحساب بينهما مفتوح'),
    L('supply','سلم المورد شحنة قطع قيمتها 1.260.000 دج في 21/01/2025'),
    L('balance','ما زال جزء من حساب الشحنة غير مسدد والحساب محل خلاف'),
    Q('pay','يطلب المورد تصفية الحساب وإلزام العميل بالرصيد المستحق')],
  [E('goods_delivered',['supply'],actor='plaintiff',date='21/01/2025',amount='1260000'),E('price_unpaid',['balance'],actor='defendant')],
  [R('payment',['pay'])],[
    C('دفع العميل قسطا في 08/02/2025',actor='defendant',date='08/02/2025'),
    C('لا يوجد دفع جزئي؛ لم يسدد العميل شيئا من قيمة الشحنة',actor='defendant'),
    C(['يحتج العميل بقسط يقول إنه دفعه يوم 08/02/2025','ينكر المورد أن العميل دفع ذلك القسط'],actor='defendant',date='08/02/2025'),
    C(None)],court='القسم التجاري')

P('f21', ('commercial_lease','lessor'), [
    L('role','المدعي مؤجر محل تجاري والمدعى عليه المستأجر الذي يستغل المحل'),
    L('lease','أبرم الإيجار التجاري في 05/05/2021 ببدل شهري 62.000 دج'),
    L('arrears','لم يدفع المستأجر بدلات قدرها 434.000 دج'),
    Q('ask','يطلب المؤجر إخلاء المحل ودفع المتأخرات البالغة 434.000 دج')],
  [E('contract_concluded',['lease'],date='05/05/2021',amount='62000'),E('rent_unpaid',['arrears'],actor='defendant',amount='434000')],
  [R('eviction',['ask']),R('payment',['ask'],'434000')],[
    C('وجه المؤجر للمستأجر تنبيها بإخلاء المحل يوم 09/02/2024',actor='plaintiff',date='09/02/2024'),
    C('لم يوجه المؤجر تنبيها بالإخلاء، ولا يوجد في الملف إخطار بإنهاء الشغل',actor='plaintiff'),
    C('يقر المستأجر بالعلاقة التجارية لكنه ينازع في توجيه المؤجر التنبيه بالإخلاء المؤرخ في 09/02/2024',actor='plaintiff',date='09/02/2024'),
    C(None)],court='القسم التجاري')

def commercial_binding(cd, monthly, nd, refusal, total, cost, doc=False, early_notice=False, expert=None):
    lines=[L('role','المدعي مستأجر يستغل المحل التجاري والمدعى عليه هو مالكه المؤجر'),
              L('lease',f'أبرم عقد الإيجار التجاري في {cd} ببدل شهري {monthly} دج'),
              L('paid','تؤكد الملاحظات أن المستأجر دفع جميع البدلات المستحقة ولم تبق متأخرات'),
              (D if doc else L)('notice',f'تنبيه من المؤجر المدعى عليه بإخلاء المستأجر للمحل في {nd}'),
              L('refusal',refusal),L('cost',f'فاتورة نقل التجهيزات بمبلغ {cost} دج'),
              Q('ask',f'يلتمس المستأجر {total} دج تعويض استحقاق')]
    values=[N('amount',cost,'moving expenses, not compensation total',['cost'])]
    if early_notice:
        lines=[lines[0],lines[3],lines[4],lines[2],lines[1],*lines[5:]]
    if expert:
        lines.append(L('expert',f'قدر الخبير قيمة التعويض بـ{expert} دج، ويعترض المستأجر على هذا التقدير'))
        values.append(N('amount',expert,'expert estimate, not relief actually requested',['expert']))
    return V(lines,
             [E('contract_concluded',['lease'],date=cd,amount=monthly),E('rent_unpaid',['paid'],polarity='denied',actor='plaintiff'),E('eviction_notice',['notice'],actor='defendant',date=nd),E('lease_renewal_refused',['refusal'],actor='defendant')],
             [R('damages',['ask'],total)],values)

B('f22',('commercial_lease','lessee'),[
    commercial_binding('12/06/2017','48000','03/04/2024','رفض المؤجر تجديد الإيجار رغم طلب المستأجر','1250000','96000'),
    commercial_binding('08/09/2016','75000','18/12/2023','قرر المالك عدم تجديد العقد للمستأجر','1800000','150000',doc=True),
    commercial_binding('29/01/2019','53000','14/05/2025','رفض المالك تجديد إيجار المحل','1420000','106000',early_notice=True),
    commercial_binding('17/03/2018','66000','22/08/2024','أبلغ المؤجر المستأجر بأنه يرفض التجديد','2100000','132000',expert='1900000')],court='القسم التجاري')

P('f23', ('sale','buyer'), [
    L('role','المدعية مشترية مولد كهربائي من المدعى عليه البائع'),
    L('sale','وقع البيع في 08/08/2024 بثمن 670.000 دج'),
    L('pay','دفعت المشترية تسبيقا قدره 170.000 دج في 10/08/2024'),
    L('due','انقضى أجل التسليم في 30/08/2024 ولم يسلم البائع المولد'),
    Q('ask','تطلب إلزام البائع بتسليم المولد فقط')],
  [E('contract_concluded',['sale'],date='08/08/2024',amount='670000'),E('payment_made',['pay'],actor='plaintiff',date='10/08/2024',amount='170000'),E('deadline_passed',['due'],date='30/08/2024'),E('delivery_missing',['due'],actor='defendant')],
  [R('delivery',['ask'])],[
    C('وجهت المشترية للبائع إعذارا بالتسليم في 12/09/2024',actor='plaintiff',date='12/09/2024'),
    C('لم تصدر عن المشترية مطالبة سابقة بالتسليم أو إعذار للبائع',actor='plaintiff'),
    C('ينازع البائع في أنه وجه إليه الإعذار بالتسليم الذي تتمسك به المشترية بتاريخ 12/09/2024',actor='plaintiff',date='12/09/2024'),
    C(None)])

P('f24', ('sale','buyer'), [
    L('role','المدعي اشترى من المدعى عليه مضخة ماء والثمن يستحق عند التسليم بحسب اتفاقهما'),
    L('sale','أبرم البيع في 15/01/2024 بثمن 390.000 دج'),
    L('due','حل موعد التسليم في 15/02/2024 ولم تسلم المضخة'),
    Q('ask','يطلب المشتري إلزام البائع بالتسليم فقط')],
  [E('contract_concluded',['sale'],date='15/01/2024',amount='390000'),E('deadline_passed',['due'],date='15/02/2024'),E('delivery_missing',['due'],actor='defendant')],
  [R('delivery',['ask'])],[
    C('عجل المشتري رغم شرط الاستحقاق بدفع 90.000 دج في 20/01/2024',actor='plaintiff',date='20/01/2024',amount='90000'),
    C('لم يعجل المشتري بأي مبلغ ولم يدفع جزءا من الثمن إلى اليوم',actor='plaintiff'),
    C('يؤكد المشتري أنه دفع 90.000 دج في 20/01/2024، ويقول البائع إن هذا الدفع لم يقع',actor='plaintiff',date='20/01/2024',amount='90000'),
    C(None)])

def divorce_binding(marriage, side, ask, md, visits, doc=False, values=()):
    return V([L('role','المدعي هو الزوج ويطلب إنهاء زواجه بالمدعى عليها'),
              (D if doc else L)('marriage',marriage),L('side',side),Q('ask',ask)],
             [E('marriage',['role','marriage'],date=md,date_cites=['marriage'])],
             [R('divorce',['ask'])]+([R('visitation',['ask'])] if visits else []),values,
             'Owner substitution: realistic marriage affirmed; contrasts concern sources/dates and requests')

B('f25',('marriage','husband'),[
    divorce_binding('تزوج الطرفان في 03/02/2012 وفقا لعقد الزواج',
      'غادرت الزوجة بيت الزوجية في 12/09/2024، واستخرج الزوج نسخة العقد في 21/10/2024',
      'يلتمس الحكم بالطلاق وتمكينه من زيارة ابنه الصغير','03/02/2012',True,
      values=[N('date','12/09/2024','separation, not marriage',['side']),N('date','21/10/2024','marriage copy date',['side'])]),
    divorce_binding('عقد زواج الطرفين مؤرخ في 11/06/2014',
      'طلق الزوج زوجة أخرى في 07/04/2011، وانفصل عن المدعى عليها في 18/08/2023',
      'طلبه هو الطلاق وزيارة ابنتهما','11/06/2014',True,
      values=[N('date','07/04/2011','divorce from a third party',['side']),N('date','18/08/2023','current separation',['side'])]),
    divorce_binding('مستخرج عقد زواج الطرفين: انعقد الزواج في 15/01/2017، وحرر المستخرج في 07/03/2025',
      'الطرفان منفصلان في السكن منذ 02/11/2024، وليس لهما أطفال',
      'يطلب الزوج الحكم بالطلاق فقط','15/01/2017',False,doc=True,
      values=[N('date','07/03/2025','extract issue date',['marriage']),N('date','02/11/2024','separation date',['side'])]),
    divorce_binding('نسخة سجل الزواج المستخرجة يوم 09/06/2025 تثبت زواج الطرفين بتاريخ 27/03/2019، وقيدها رقم 54318',
      'أول جلسة إصلاح عائلية بالمكتب كانت في 14/05/2025؛ الأبناء راشدون',
      'يلتمس الزوج الطلاق دون طلبات أخرى','27/03/2019',False,doc=True,
      values=[N('date','09/06/2025','registry copy date',['marriage']),N('identifier','54318','marriage register entry',['marriage']),N('date','14/05/2025','office meeting date',['side'])])],court='قسم شؤون الأسرة')

P('f26', ('marriage','wife'), [
    L('role','المدعية زوجة المدعى عليه وتلتمس التطليق بسبب انقطاع النفقة'),
    L('marriage','انعقد زواجهما في 14/04/2016'),
    L('maintenance','امتنع الزوج عن الإنفاق عليها'),
    Q('ask','تطلب التطليق ونفقة متأخرة قدرها 240.000 دج')],
  [E('marriage',['marriage'],date='14/04/2016'),E('maintenance_unpaid',['maintenance'],actor='defendant')],
  [R('tatliq',['ask']),R('maintenance',['ask'],'240000')],[
    C('هجر المدعى عليه زوجته وترك البيت دون عودة',actor='defendant'),
    C('لم يهجر الزوج زوجته ولم يترك المسكن؛ الامتناع عن النفقة قائم مع إقامته فيه',actor='defendant'),
    C(['تقول الزوجة إن زوجها هجرها وترك المسكن','ينكر الزوج واقعة الهجر التي تستند إليها إلى جانب انقطاع النفقة'],actor='defendant'),
    C(None)],court='قسم شؤون الأسرة')

P('f27', ('marriage','wife'), [
    L('role','المدعية زوجة تطلب التطليق من المدعى عليه لامتناعه عن الإنفاق'),
    D('marriage','عقد زواج الطرفين بتاريخ 22/07/2018؛ صدرت نسخته في 11/02/2025'),
    L('maintenance','لم ينفق الزوج على المدعية رغم حاجتها'),
    Q('ask','تلتمس التطليق ونفقة متأخرة قدرها 315.000 دج وتعويضا قدره 120.000 دج')],
  [E('marriage',['role','marriage'],date='22/07/2018',date_cites=['marriage']),E('maintenance_unpaid',['maintenance'],actor='defendant')],
  [R('tatliq',['ask']),R('maintenance',['ask'],'315000'),R('damages',['ask'],'120000')],[
    C('ضرب الزوج زوجته وألحق بها أذى جسديا',actor='defendant'),
    C('لم يضرب الزوج زوجته ولم يصدر عنه إيذاء',actor='defendant'),
    C('ينكر الزوج ما تتمسك به الزوجة من أنه ضربها وأذاها',actor='defendant'),
    C(None)],court='قسم شؤون الأسرة')

def khul_binding(marriage, facts, asks, md, offer, maintenance, housing, custody, values, doc=False):
    return V([L('role','المدعية زوجة المدعى عليه وتطلب الخلع منه مقابل بدل تعرضه'),
              (D if doc else L)('marriage',marriage),L('detail',facts)]+[Q('ask'+str(i+1),s) for i,s in enumerate(asks)],
             [E('marriage',['role','marriage'],date=md,date_cites=['marriage'])],
             [R('khul',['ask1'],offer)]+([R('maintenance',['ask2'],maintenance)] if maintenance else [])+
             ([R('housing',['ask3'],housing)] if housing else [])+([R('custody',['ask4'])] if custody else []),values,
             'Marriage remains affirmed. The maintenance amount is the expressly written aggregate, not an inferred sum')

B('f28',('marriage','wife'),[
    khul_binding('انعقد زواج الطرفين في 06/09/2015','تركت الزوجة المسكن في 17/01/2025 ولهما طفلان',[
      'تعرض المدعية بدل خلع قدره 70.000 دج وتطلب الحكم بالخلع',
      'نفقة العدة 30.000 دج ومتأخر نفقة الطفلين 90.000 دج؛ تلتمس أداء 120.000 دج عنهما، ونفقة الطفلين 15.000 دج في الشهر',
      'تلتمس 72.000 دج بدل سكن عن الفترة؛ كراء المسكن 12.000 دج في الشهر',
      'وتطلب إسناد حضانة الطفلين إليها'], '06/09/2015','70000','120000','72000',True,
      [N('date','17/01/2025','separation date',['detail']),N('amount','30000','waiting-period component of maintenance',['ask2']),N('amount','90000','child arrears component',['ask2']),N('amount','15000','monthly child rate',['ask2']),N('amount','12000','monthly housing rate',['ask3'])]),
    khul_binding('عقد زواج بتاريخ 19/03/2017؛ المستخرج مؤرخ في 08/04/2025','لها طفلة وتقيم منفصلة منذ 12/02/2025',[
      'تطلب الخلع على بدل تعرضه بمقدار 85.000 دج',
      'تلتمس 42.000 دج نفقة عدة فقط؛ نفقة الطفلة السابقة 18.000 دج في الشهر',
      'تلتمس 14.000 دج كل شهر بدل إيجار',
      'تلتمس حضانة الطفلة'], '19/03/2017','85000','42000','14000',True,
      [N('date','08/04/2025','extract date',['marriage']),N('date','12/02/2025','separation date',['detail']),N('amount','18000','previous child maintenance, not this request',['ask2'])],doc=True),
    khul_binding('تزوج الطرفان يوم 24/11/2020','لا أبناء لهما، ومهر الزواج القديم 60.000 دج',[
      'تلتمس الخلع مقابل بدل تعرضه الآن قدره 110.000 دج',
      'تلتمس نفقة عدة قدرها 55.000 دج'], '24/11/2020','110000','55000',None,False,
      [N('amount','60000','historical dowry, not khul offer',['detail'])]),
    khul_binding('نسخة زواج صادرة في 02/06/2025 عن عقد زواج الطرفين المؤرخ في 13/08/2013','للزوجين ابن وتقيم الزوجة منفصلة عنه منذ 03/03/2025',[
      'تعرض 95.000 دج بدلا للخلع وتطلب الحكم به',
      'تلتمس أداء 168.000 دج للنفقة؛ نفقة عدتها 48.000 دج ومتأخر نفقة الابن 120.000 دج، ونفقته 20.000 دج في الشهر',
      'لا تطلب سكنا أو بدل إيجار في هذه الدعوى',
      'تطلب حضانة الابن'], '13/08/2013','95000','168000',None,True,
      [N('date','02/06/2025','marriage copy date',['marriage']),N('date','03/03/2025','separation date',['detail']),N('amount','48000','waiting-period component',['ask2']),N('amount','120000','child arrears component',['ask2']),N('amount','20000','monthly child rate',['ask2'])],doc=True)],court='قسم شؤون الأسرة')

P('f29', ('marriage','wife'), [
    L('role','المدعية أم الطفل من علاقتها الزوجية بالمدعى عليه وتطالبه بنفقة الطفل'),
    L('marriage','أبرم زواج الطرفين في 09/10/2014'),
    L('unpaid','امتنع الأب المدعى عليه عن الإنفاق على الطفل؛ تقيم المدعية مع الطفل منفصلة عنه في السكن'),
    Q('ask','تلتمس 180.000 دج نفقة متأخرة للطفل')],
  [E('marriage',['marriage'],date='09/10/2014'),E('maintenance_unpaid',['unpaid'],actor='defendant')],[R('maintenance',['ask'],'180000')],[
    C('وقع الطلاق بين الطرفين بحكم مؤرخ في 21/06/2022',date='21/06/2022'),
    C('لم يقع طلاق بين الطرفين وهما لا يزالان زوجين رغم انفصالهما في السكن'),
    C(['تستند الأم إلى طلاق تقول إنه وقع في 21/06/2022','ينكر الأب وقوع الطلاق ويقول إن العلاقة الزوجية لم تنحل'],date='21/06/2022'),
    C(None)],court='قسم شؤون الأسرة')

def maintenance_binding(md, divorce, dd, detail, ask, amount, housing, values=()):
    lines=[L('role','المدعية أم طفل من علاقتها الزوجية بالمدعى عليه وتطلب منه النفقة'),D('marriage',f'عقد زواج الطرفين يوم {md}'),L('detail',detail),Q('ask',ask)]
    events=[E('marriage',['role','marriage'],date=md,date_cites=['marriage']),E('maintenance_unpaid',['detail'],actor='defendant')]
    if divorce:
        lines.append(D('divorce',divorce));events.append(E('divorce',['divorce'],date=dd))
    else:
        lines.append(L('divorce','لم يقع طلاق بين الطرفين ولا تزال الزوجية قائمة'));events.append(E('divorce',['divorce'],polarity='denied'))
    requests=[R('maintenance',['ask'],amount)]+([R('housing',['ask'],housing)] if housing else [])
    return V(lines,events,requests,values)

B('f30',('marriage','wife'),[
    maintenance_binding('05/05/2011','حكم الطلاق الصادر يوم 14/09/2021، استخرجت نسخته في 09/01/2025','14/09/2021',
      'امتنع الأب عن النفقة؛ نفقة الطفل 16.000 دج كل شهر، وعليه 128.000 دج متأخرات',
      'تلتمس 128.000 دج نفقة متأخرة و80.000 دج بدل سكن؛ كراء المسكن 10.000 دج في الشهر','128000','80000',
      [N('date','09/01/2025','divorce copy date',['divorce']),N('amount','16000','monthly maintenance rate',['detail']),N('amount','128000','arrears explicitly repeated in request',['detail']),N('amount','10000','monthly housing rate',['ask'])]),
    maintenance_binding('18/02/2018',None,None,
      'لم ينفق الزوج على المدعية منذ انفصالهما في المسكن؛ قدر الاحتياج الشهري بـ22.000 دج',
      'تلتمس 154.000 دج نفقة متأخرة فقط','154000',None,
      [N('amount','22000','monthly need, not requested aggregate',['detail'])]),
    maintenance_binding('27/06/2013','حكم الطلاق مؤرخ في 08/10/2020 ورقم فهرسته 68214','08/10/2020',
      'الأب لم ينفق على الطفلين؛ نفقتهما 24.000 دج في الشهر وعليه 216.000 دج متأخرات',
      'تلتمس 216.000 دج نفقة عن الفترة و18.000 دج كل شهر بدل سكن','216000','18000',
      [N('identifier','68214','judgment registry identifier',['divorce']),N('amount','24000','monthly maintenance rate',['detail']),N('amount','216000','expressly stated arrears',['detail'])]),
    maintenance_binding('16/12/2016','قضى الحكم الصادر في 11/03/2023 بالطلاق، واستخرج في 07/07/2024','11/03/2023',
      'امتنع الأب عن دفع نفقة الطفل؛ دفعت الأم إيجارا بمبلغ 13.000 دج شهريا من مالها',
      'تلتمس 195.000 دج نفقة متأخرة و78.000 دج بدل سكن عن الفترة','195000','78000',
      [N('date','07/07/2024','judgment extract date',['divorce']),N('amount','13000','monthly rent paid by mother',['detail'])])],court='قسم شؤون الأسرة')

P('f31', ('marriage','mother'), [
    L('role','المدعية أم الطفلين والمدعى عليه أبوهما، وهي تلتمس حضانتهما'),
    D('divorce','حكم طلاق الطرفين الصادر في 04/04/2022'),
    L('side','تؤكد الملاحظات أن المدرسة قريبة من مسكن الأم'),
    Q('ask','تلتمس إسناد حضانة الطفلين والولاية عليهما للمدعية')],
  [E('divorce',['divorce'],date='04/04/2022')],[R('custody',['ask']),R('guardianship',['ask'])],[
    C('يقيم الطفلان حاليا عند أمهما المدعية',actor='plaintiff'),
    C('الطفلان يقيمان حاليا في مركز رعاية ولا يقيمان عند أي من الوالدين'),
    C(['تتمسك الأم بأن الطفلين يقيمان عندها حاليا','ينكر الأب إقامتهما عند الأم ويقول إنهما عند الجدة'],actor='plaintiff'),
    C(None)],court='قسم شؤون الأسرة')

P('f32', ('marriage','father'), [
    L('role','المدعي أب المحضون والمدعى عليها أمه التي أسندت إليها الحضانة سابقا'),
    D('judgment','حكم إسناد حضانة الطفل للأم بتاريخ 19/09/2020'),
    L('unfit','تؤكد ملاحظات الأب أن الأم أهملت رعاية الطفل وتعليمه بصورة تعرضه للخطر'),
    Q('ask','يلتمس إسقاط حضانة الأم ونقلها إليه ومنحه الولاية')],
  [E('custody_judgment',['judgment'],date='19/09/2020'),E('custodian_unfit',['unfit'],actor='defendant')],
  [R('custody_transfer',['ask']),R('guardianship',['ask'])],[
    C('تزوجت الأم الحاضنة في 13/02/2024 برجل أجنبي عن الطفل ليس من محارمه',actor='defendant',date='13/02/2024'),
    C('لم تتزوج الأم الحاضنة من جديد ولم ينعقد لها زواج آخر',actor='defendant'),
    C('ينسب الأب إلى الأم زواجا بأجنبي عن الطفل في 13/02/2024، وتنكر الأم وقوع هذا الزواج',actor='defendant',date='13/02/2024'),
    C(None)],court='قسم شؤون الأسرة')

P('f33', ('marriage','mother'), [
    L('role','المدعية أم الطفل والمدعى عليه أبوه الذي كان حاضنا له'),
    D('judgment','حكم إسناد الحضانة للأب بتاريخ 07/07/2021'),
    L('renounce','تنازل الأب عن حضانة الطفل في 26/04/2024؛ تطلب الأم نقلها إليها لهذا السبب'),
    Q('ask','تلتمس إسقاط الحضانة عن الأب ونقلها إلى الأم')],
  [E('custody_judgment',['judgment'],date='07/07/2021'),E('custody_renounced',['renounce'],actor='defendant',date='26/04/2024')],[R('custody_transfer',['ask'])],[
    C('استوطن الأب الحاضن بلدا أجنبيا ونقل مقر إقامته المعتاد إليه',actor='defendant'),
    C('لم يستوطن الأب بلدا أجنبيا ولا ينوي الاستيطان خارج الوطن؛ كان سفره زيارة قصيرة فقط',actor='defendant'),
    C('تتمسك الأم بأن الأب استوطن الخارج، وهو ينازع في ذلك ويقول إن سفره مؤقت',actor='defendant'),
    C(None)],court='قسم شؤون الأسرة')

P('f34', ('marriage','father'), [
    L('role','المدعي أب الطفل والمدعى عليها أمه ويطلب تمكينه من زيارته'),
    L('divorce','وقع الطلاق بين الطرفين في 12/12/2021'),
    L('refusal','منعت الأم المدعي من رؤية الطفل رغم محاولاته الودية'),
    Q('ask','يلتمس تقرير حقه في زيارة الطفل وتمكينه منه')],
  [E('divorce',['divorce'],date='12/12/2021'),E('visitation_refused',['refusal'],actor='defendant')],[R('visitation',['ask'])],[
    C('صدر حكم سابق حدد للأب حق الزيارة في 09/03/2022',date='09/03/2022'),
    C('لم يصدر حكم سابق يحدد حق الزيارة؛ وهذا أول طلب قضائي لتحديدها'),
    C('يستند الأب إلى حكم يقول إنه حدد الزيارة في 09/03/2022، وتنازع الأم في صدور حكم الزيارة الذي يذكره',date='09/03/2022'),
    C(None)],court='قسم شؤون الأسرة')

def dismissal_binding(start, dismissal, conciliation, ask, ed, dd, cd, amount, reinstate, dispute=False, doc=False, values=()):
    return V([L('role','المدعي عامل لدى المدعى عليها المستخدمة ويطالبها بصفته العامل'),L('start',start),
              (D if doc else L)('dismissal',dismissal),D('conciliation',conciliation),
              L('context1','كان العامل يشتغل في قسم التوضيب ثم نقل داخل المؤسسة إلى مراقبة المخزون، ويذكر أن اسمه بقي في لوائح الحضور بعد تغيير القسم؛ استمر في خدمة المستخدمة نفسها دون انقطاع رغم تغيير مهمته داخل الورشة'),
              L('context2','أرفق العامل أوراق الحضور ومراسلاته مع المستخدمة بشأن الوقائع المهنية التي نسبت إليه وإنهاء العمل'),
              L('context3','زار العامل مقر المؤسسة لاسترجاع أغراضه الشخصية وسلم نسخا من أوراقه لممثل العمال'),
              L('context4','توجد في الملف كشوف أجر قديمة ومراسلات عن توزيع المناوبات، وتتضمن المراسلات خلافا على من كان يدير المناوبات في الفترة الأخيرة؛ طلب العامل إرفاق كشف المناوبات مع أوراق الملف'),Q('ask',ask)],
             [E('employment_started',['start'],date=ed),E('dismissal',['dismissal'],actor='defendant',polarity='disputed' if dispute else 'affirmed',date=dd),E('conciliation_failed',['conciliation'],date=cd)],
             [R('damages',['ask'],amount)]+([R('reinstatement',['ask'])] if reinstate else []),values,
             'Contesting the reason leaves the dismissal affirmed; contesting the act makes it disputed')

B('f35',('employment','employee'),[
    dismissal_binding('بدأ المدعي العمل لدى المدعى عليها في 03/05/2017 وكان أجره الشهري 48.000 دج',
      'سرحته المستخدمة في 11/09/2024 بدعوى ارتكابه خطأ جسيما، وينكر العامل ارتكاب هذا الخطأ',
      'محضر عدم المصالحة المحرر في 06/11/2024، رقم المحضر 70518',
      'يلتمس إعادة الإدماج وتعويضا قدره 780.000 دج','03/05/2017','11/09/2024','06/11/2024','780000',True,
      values=[N('amount','48000','monthly wage, not damages',['start']),N('identifier','70518','conciliation record identifier',['conciliation'])]),
    dismissal_binding('التحق المدعي بالعمل في 21/01/2019 بأجر شهري 55.000 دج',
      'يقول العامل إن المستخدمة فصلته في 17/02/2025؛ تنكر المستخدمة أنها فصلته وتقول إنه استقال',
      'محضر عدم مصالحة بتاريخ 08/04/2025',
      'يطلب تعويضا عن الفصل قدره 1.100.000 دج فقط','21/01/2019','17/02/2025','08/04/2025','1100000',False,dispute=True,
      values=[N('amount','55000','monthly wage',['start'])]),
    dismissal_binding('بدأ العامل عمله يوم 14/06/2016؛ كشوف الأجر تشير إلى 42.000 دج شهريا',
      'قرار فصل العامل الصادر عن المستخدمة يوم 29/07/2024، استخرجت صورة القرار في 12/08/2024',
      'محضر عدم المصالحة في 19/09/2024، استخرجت نسخة ثانية منه في 09/01/2025',
      'يلتمس إعادة إدماجه وتعويضا مقداره 630.000 دج','14/06/2016','29/07/2024','19/09/2024','630000',True,doc=True,
      values=[N('amount','42000','monthly wage',['start']),N('date','12/08/2024','dismissal copy date',['dismissal']),N('date','09/01/2025','conciliation copy date',['conciliation'])]),
    dismissal_binding('في 18/03/2020 بدأ العمل بأجر شهري 61.000 دج',
      'فصلت المستخدمة المدعي في 07/10/2024 لسبب مهني يعترض عليه العامل',
      'محضر فشل المصالحة المحرر في 23/12/2024',
      'يلتمس تعويضا قدره 915.000 دج فقط','18/03/2020','07/10/2024','23/12/2024','915000',False,
      values=[N('amount','61000','monthly wage, not requested total',['start'])])],court='القسم الاجتماعي')

P('f36', ('employment','employee'), [
    L('role','المدعية عاملة لدى المدعى عليها المستخدمة وتطالب بأجورها'),
    L('start','التحقت بالعمل في 02/02/2020 بأجر شهري 37.000 دج'),
    L('unpaid','لم تدفع المستخدمة أجور العاملة عن الفترة المطالب بها، وتبلغ أجورها عنها 222.000 دج'),
    Q('ask','تلتمس إلزام المستخدمة بدفع 222.000 دج أجورا متأخرة')],
  [E('employment_started',['start'],date='02/02/2020'),E('wages_unpaid',['unpaid'],actor='defendant',amount='222000')],[R('wages',['ask'],'222000')],[
    C('محضر عدم مصالحة بين العاملة والمستخدمة محرر في 18/03/2025',date='18/03/2025',field='documents.list'),
    C('لم يحرر محضر عدم مصالحة بعد؛ نجهز ملف الأجور بالمكتب ريثما يرد جواب جهة المصالحة',note='Realistic preparation notes with pending procedural record; substantive wages claim remains asserted'),
    C('تتمسك العاملة بصدور محضر عدم مصالحة في 18/03/2025، والمستخدمة تنكر صدور هذا المحضر',date='18/03/2025'),
    C(None)],court='القسم الاجتماعي')
