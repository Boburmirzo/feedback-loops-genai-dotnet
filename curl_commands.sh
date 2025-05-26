curl -X POST "http://localhost:7071/api/add-podcast" \\ -H "Content-Type: application/json" \\ -d '{ "title": "Future of Robotics", "transcript": "This episode discusses the advancements in robotics and how automation is transforming industries worldwide. We explore the latest innovations in robotic technologies, including machine learning and AI integration, enabling robots to perform complex tasks with precision. Additionally, we discuss the ethical implications and challenges of widespread automation, such as its impact on the workforce and society at large." }'

curl -X POST "http://localhost:7071/api/update-user-history" \\ -H "Content-Type: application/json" \\ -d '{"userId": 13, "listeningHistory": "Interested in robotics and AI."}'

curl -X GET "http://localhost:7071/api/recommend-podcasts?userId=13"

curl -X GET "http://localhost:7071/api/get-suggested-podcasts?userId=13"


## Add more podcast episodes

curl -X POST "http://localhost:7071/api/add-podcast" -H "Content-Type: application/json" -d '{"title": "The Rise of Quantum Computing", "transcript": "We explore the fundamentals of quantum computing and its potential to revolutionize industries from pharmaceuticals to finance."}'

curl -X POST "http://localhost:7071/api/add-podcast" -H "Content-Type: application/json" -d '{"title": "Climate Tech Innovations", "transcript": "This episode focuses on cutting-edge technologies designed to combat climate change, from carbon capture to green hydrogen."}'

curl -X POST "http://localhost:7071/api/add-podcast" -H "Content-Type: application/json" -d '{"title": "AI in Healthcare", "transcript": "How artificial intelligence is helping doctors diagnose and treat diseases more accurately and efficiently."}'

curl -X POST "http://localhost:7071/api/add-podcast" -H "Content-Type: application/json" -d '{"title": "Space Tourism is Here", "transcript": "We discuss recent milestones in commercial space travel and what it means for the future of exploration."}'

curl -X POST "http://localhost:7071/api/add-podcast" -H "Content-Type: application/json" -d '{"title": "The Ethics of AI", "transcript": "A deep dive into the ethical questions surrounding artificial intelligence, including bias, privacy, and accountability."}'

curl -X POST "http://localhost:7071/api/add-podcast" -H "Content-Type: application/json" -d '{"title": "Renewable Energy Myths", "transcript": "Debunking common misconceptions about solar, wind, and other renewable energy sources."}'

curl -X POST "http://localhost:7071/api/add-podcast" -H "Content-Type: application/json" -d '{"title": "Future of Urban Mobility", "transcript": "We examine how smart transportation and electric vehicles are reshaping cities."}'

curl -X POST "http://localhost:7071/api/add-podcast" -H "Content-Type: application/json" -d '{"title": "Cybersecurity in a Connected World", "transcript": "With the rise of IoT, we explore the growing need for robust cybersecurity measures."}'

curl -X POST "http://localhost:7071/api/add-podcast" -H "Content-Type: application/json" -d '{"title": "Blockchain Beyond Crypto", "transcript": "How blockchain is being used in supply chains, voting systems, and beyond."}'

curl -X POST "http://localhost:7071/api/add-podcast" -H "Content-Type: application/json" -d '{"title": "The Future of Work", "transcript": "Remote work, automation, and the gig economy—what the workforce of tomorrow looks like."}'
