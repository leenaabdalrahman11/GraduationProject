import React from "react";
import Navbar from "../components/user/navbar/Navbar";
import Footer from "../components/user/footer/Footer";
import { Outlet, useLocation } from "react-router-dom";

export default function Layout() {
  const location = useLocation();

  const hideNavbar = location.pathname === "/homeBlind";

  return (
    <>
      {!hideNavbar && <Navbar />}
      <div>
        <Outlet />
      </div>
      <Footer />
    </>
  );
}